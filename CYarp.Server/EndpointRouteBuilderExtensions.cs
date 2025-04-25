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
    /// 终结点路由扩展
    /// </summary>
    public static class EndpointRouteBuilderExtensions
    {
        private static readonly string[] cyarpMethods = [HttpMethods.Get, HttpMethods.Connect];

        /// <summary>
        /// 处理CYarp的客户端连接
        /// </summary>
        /// <typeparam name="TClientIdProvider">客户端id的提供者</typeparam>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TClientIdProvider>(this IEndpointRouteBuilder endpoints) where TClientIdProvider : IClientIdProvider
        {
            var clientIdProvider = ActivatorUtilities.CreateInstance<TClientIdProvider>(endpoints.ServiceProvider);
            return endpoints.MapCYarp(clientIdProvider.GetClientIdAsync);
        }

        /// <summary>
        /// 处理CYarp的客户端连接
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="clientIdProvider">客户端id的提供者</param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp(this IEndpointRouteBuilder endpoints, Func<HttpContext, string?> clientIdProvider)
        {
            ArgumentNullException.ThrowIfNull(clientIdProvider);
            return endpoints.MapCYarp(context => ValueTask.FromResult(clientIdProvider(context)));
        }

        /// <summary>
        /// 处理CYarp的客户端连接
        /// </summary>
        /// <param name="endpoints"></param>
        /// <param name="clientIdProvider">客户端id的提供者</param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp(this IEndpointRouteBuilder endpoints, Func<HttpContext, ValueTask<string?>> clientIdProvider)
        {
            ArgumentNullException.ThrowIfNull(clientIdProvider);

            var cyarp = endpoints.MapGroup("/cyarp");

            // Tunnel的连接处理
            cyarp.MapMethods("/{tunnelId}", cyarpMethods, TunnelHanlder.HandleTunnelAsync).AllowAnonymous();

            // Client的连接处理
            var clientHandler = new ClientHandler(clientIdProvider);
            return cyarp.MapMethods("/", cyarpMethods, clientHandler.HandleClientAsync);
        }
    }
}
