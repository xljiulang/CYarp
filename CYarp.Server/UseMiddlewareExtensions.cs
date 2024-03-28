using CYarp.Server.Middlewares;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Microsoft.AspNetCore.Builder
{
    /// <summary>
    /// 中间件扩展
    /// </summary>
    public static class UseMiddlewareExtensions
    {
        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app)
        {
            return app.UseCYarpCore(null);
        }

        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <param name="policyName">IClient授权验证策略名</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app, string policyName)
        {
            var policyProvider = app.ApplicationServices.GetRequiredService<IAuthorizationPolicyProvider>();
            var policy = policyProvider.GetPolicyAsync(policyName).Result;
            return app.UseCYarpCore(policy);
        }

        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configurePolicy">IClient授权验证策略配置</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app, Action<AuthorizationPolicyBuilder> configurePolicy)
        {
            var builder = new AuthorizationPolicyBuilder();
            configurePolicy(builder);
            return app.UseCYarpCore(builder.Build());
        }

        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <param name="policy">IClient授权验证策略</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app, AuthorizationPolicy policy)
        {
            return app.UseCYarpCore(policy);
        }

        private static IApplicationBuilder UseCYarpCore(this IApplicationBuilder app, AuthorizationPolicy? policy)
        {
            app.UseMiddleware<CYarpMiddleware>();

            var clientMiddleware = app.ApplicationServices.GetRequiredService<CYarpClientMiddleware>();
            clientMiddleware.SetAuthorizationPolicy(policy);
            app.Use(next => context => clientMiddleware.InvokeAsync(context, next));

            app.UseMiddleware<HttpTunnelMiddleware>();
            return app;
        }
    }
}
