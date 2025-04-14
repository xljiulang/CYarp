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
    /// 应用扩展
    /// </summary>
    public static class ApplicationBuilderExtensions
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
        /// 注册CYarp的路由
        /// </summary>
        /// <param name="endpoints"></param>
        /// <returns></returns>
        public static IEndpointConventionBuilder MapCYarp(this IEndpointRouteBuilder endpoints)
        { 
            var httpTunnelHandler = endpoints.ServiceProvider.GetRequiredService<HttpTunnelHanlder>();
            endpoints.MapGet("/{tunnelId}", httpTunnelHandler.InvokeAsync).AllowAnonymous();

            var clientHandler = endpoints.ServiceProvider.GetRequiredService<ClientHandler>();
            var clientBuilder = endpoints.MapGet("/cyarp", clientHandler.InvokeAsync);

            clientBuilder.Finally(endpoint =>
            {
                if (!endpoint.Metadata.Any((object meta) => meta is IAuthorizeData))
                {
                    var options = endpoints.ServiceProvider.GetRequiredService<IOptions<CYarpOptions>>();
                    endpoint.Metadata.Add(new AuthorizeAttribute());
                    endpoint.Metadata.Add(options.Value.Authorization.GetAuthorizationPolicy());
                }
            });

            return clientBuilder;
        }
    }
}
