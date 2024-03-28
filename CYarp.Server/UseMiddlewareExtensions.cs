using CYarp.Server.Clients;
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
            app.UseMiddleware<CYarpMiddleware>();
            app.UseMiddleware<CYarpClientMiddleware>();
            app.UseMiddleware<HttpTunnelMiddleware>();
            return app;
        }

        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <param name="clientPolicy">IClient授权验证策略配置</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app, AuthorizationPolicy clientPolicy)
        {
            app.Client().SetPolicy(clientPolicy);
            return app.UseCYarp();
        }

        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <param name="clientPolicyName">IClient授权验证策略名</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app, string clientPolicyName)
        {
            app.Client().SetPolicy(clientPolicyName);
            return app.UseCYarp();
        }

        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <param name="configureClientPolicy">IClient授权验证策略配置</param>
        /// <returns></returns>
        public static IApplicationBuilder UseCYarp(this IApplicationBuilder app, Action<AuthorizationPolicyBuilder> configureClientPolicy)
        {
            var builder = new AuthorizationPolicyBuilder();
            configureClientPolicy(builder);
            var clientPolicy = builder.Build();

            app.Client().SetPolicy(clientPolicy);
            return app.UseCYarp();
        }

        /// <summary>
        /// 使用CYarp中间件
        /// 自动管理IClient的连接
        /// 跳过IClient的授权验证        
        /// </summary>
        /// <param name="app"></param> 
        /// <returns></returns>
        public static IApplicationBuilder UseCYarpAnonymous(this IApplicationBuilder app)
        {
            app.Client().SetAllowAnonymous();
            return app.UseCYarp();
        }

        private static ClientAuthorization Client(this IApplicationBuilder app)
        {
            return app.ApplicationServices.GetRequiredService<ClientAuthorization>();
        }
    }
}
