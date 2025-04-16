using CYarp.Server.Middlewares;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// 应用程序扩展
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
    }
}
