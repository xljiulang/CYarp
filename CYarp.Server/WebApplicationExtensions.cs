using CYarp.Server;
using CYarp.Server.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq;

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
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public static RouteHandlerBuilder MapCYarp(this IEndpointRouteBuilder endpoints)
        {
            var cyarp = endpoints.MapGroup("/cyarp");

            // HttpTunnel的握手处理
            cyarp.Map("/{tunnelId}", HttpTunnelHanlder.HandleHttpTunnelAsync).AllowAnonymous();

            // Client的连接处理
            var client = cyarp.Map("/", ClientHandler.HandleClientAsync);

            // Client设置默认的授权策略
            client.Finally(endpoint =>
            {
                if (!endpoint.Metadata.Any(meta => meta is IAuthorizeData | meta is IAllowAnonymous))
                {
                    var policy = endpoints.ServiceProvider.GetRequiredService<IOptions<CYarpOptions>>().Value.Authorization.GetAuthorizationPolicy();
                    endpoint.Metadata.Add(new AuthorizeAttribute());
                    endpoint.Metadata.Add(policy);
                }
            });

            return client;
        }
    }
}
