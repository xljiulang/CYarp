using CYarp.Server;
using CYarp.Server.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// WebApplication扩展
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// 使用CYarp中间件 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app)
        {
            app.UseWebSockets();
            return app.UseMiddleware<CYarpMiddleware>();
        }

        /// <summary>
        /// 处理CYarp的客户端连接
        /// </summary>
        /// <typeparam name="TClientIdProvider">客户端id的提供者</typeparam>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp<TClientIdProvider>(this IEndpointRouteBuilder endpoints) where TClientIdProvider : IClientIdProvider
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

            // HttpTunnel的握手处理
            cyarp.Map("/{tunnelId}", HttpTunnelHanlder.HandleHttpTunnelAsync).AllowAnonymous();

            // Client的连接处理
            var clientHandler = new ClientHandler(clientIdProvider);
            return cyarp.Map("/", clientHandler.HandleClientAsync);
        }
    }
}
