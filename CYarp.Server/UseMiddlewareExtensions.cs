using CYarp.Server.Middlewares;

namespace Microsoft.AspNetCore.Builder
{
    public static class UseMiddlewareExtensions
    {
        /// <summary>
        /// 使用CYarp的Client连接管理中间件
        /// 连接自动添加或移除到IClientManager
        /// 此中间件需放到UseAuthentication之后
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarpClient(this IApplicationBuilder app)
        {
            app.UseMiddleware<CYarpMiddleware>();
            app.UseMiddleware<CYarpClientMiddleware>();
            app.UseMiddleware<TunnelStreamMiddleware>(); 
            return app;
        }
    }
}
