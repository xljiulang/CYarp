using CYarp.Server.Middlewares;

namespace Microsoft.AspNetCore.Builder
{
    public static class UseMiddlewareExtensions
    {
        /// <summary>
        /// 使用CYarp中间件
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app)
        {
            app.UseMiddleware<CYarpMiddleware>();
            app.UseMiddleware<CYarpClientMiddleware>();
            app.UseMiddleware<TunnelStreamMiddleware>();
            app.UseMiddleware<HttpForwardMiddleware>();
            return app;
        }
    }
}
