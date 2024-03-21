using CYarp.Server.Middlewares;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// 中间件扩展
    /// </summary>
    public static class UseMiddlewareExtensions
    {
        /// <summary>
        /// 使用CYarp的Client管理中间件
        /// 自动接收Client并添加或移除到IClientManager
        /// 此中间件需放到UseAuthentication之后
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app)
        {
            app.UseMiddleware<CYarpMiddleware>();
            app.UseMiddleware<CYarpClientMiddleware>();
            app.UseMiddleware<TunnelStreamMiddleware>(); 
            return app;
        }
    }
}
