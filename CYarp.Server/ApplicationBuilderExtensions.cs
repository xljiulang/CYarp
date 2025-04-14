using CYarp.Server.Middlewares;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.AspNetCore.Routing;
using System.Threading.Tasks;
using System.Threading;
using System;

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


        public static IEndpointConventionBuilder MapCYarp(this IEndpointRouteBuilder endpoints)
        {
            var routeGroup = endpoints.MapGroup(string.Empty);
            routeGroup.MapGet("/",)

            return routeGroup.RequireAuthorization(;
        }
    }
}
