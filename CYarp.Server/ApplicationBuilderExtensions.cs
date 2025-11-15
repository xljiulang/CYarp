using CYarp.Server.Middlewares;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// Application builder extensions
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Adds the CYarp middleware to the application's request pipeline
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
