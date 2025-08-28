using CYarp.Server.Middlewares;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// 应用程序Extension
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// UseCYarpMiddleware 
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
