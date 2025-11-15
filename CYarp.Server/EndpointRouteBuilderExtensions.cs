using CYarp.Server;
using CYarp.Server.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Endpoint routing extensions
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {
        /// <summary>
        /// Handles CYarp client connections (using a client id provider type)
        /// </summary>
        /// <typeparam name="TClientIdProvider">The provider type for client ids</typeparam>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClientIdProvider>(this IEndpointRouteBuilder endpoints) where TClientIdProvider : IClientIdProvider
        {
            var clientIdProvider = ActivatorUtilities.CreateInstance<TClientIdProvider>(endpoints.ServiceProvider);
            return endpoints.MapCYarp(clientIdProvider.GetClientIdAsync);
        }

        /// <summary>
        /// Handles CYarp client connections (using a synchronous client id provider)
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="clientIdProvider">The provider for client ids</param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp(this IEndpointRouteBuilder endpoints, Func<HttpContext, string?> clientIdProvider)
        {
            ArgumentNullException.ThrowIfNull(clientIdProvider);
            return endpoints.MapCYarp(context => ValueTask.FromResult(clientIdProvider(context)));
        }

        /// <summary>
        /// Handles CYarp client connections (using an async client id provider)
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="clientIdProvider">The provider for client ids</param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp(this IEndpointRouteBuilder endpoints, Func<HttpContext, ValueTask<string?>> clientIdProvider)
        {
            ArgumentNullException.ThrowIfNull(clientIdProvider);

            var cyarp = endpoints.MapGroup("/cyarp")
                .WithGroupName("cyarp")
                .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status200OK))
                .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status101SwitchingProtocols))
                .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status400BadRequest))
                .WithMetadata(new ProducesResponseTypeMetadata(StatusCodes.Status403Forbidden));

            // Tunnel connection handling
            cyarp.Map("/{tunnelId}", TunnelHandler.HandleTunnelAsync)
                .AllowAnonymous()
                .WithDisplayName("CYarp tunnel endpoint");

            // Client connection handling
            var clientHandler = new ClientHandler(clientIdProvider);
            return cyarp.Map("/", clientHandler.HandleClientAsync)
                .WithDisplayName("CYarp client endpoint");
        }
    }
}
