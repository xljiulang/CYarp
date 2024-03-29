using CYarp.Server;
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
        /// 使用CYarpOptions.Authorization规则进行授权
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static ICYarpAppBuilder UseCYarp(this IApplicationBuilder app)
        {
            app.UseWebSockets();
            app.UseMiddleware<CYarpMiddleware>();
            app.UseMiddleware<CYarpClientMiddleware>();
            app.UseMiddleware<HttpTunnelMiddleware>();
            return new CYarpAppBuilder(app.ApplicationServices);
        }

        /// <summary>
        /// 添加IClient的授权验证策略
        /// CYarpOptions.Authorization规则不再生效
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clientPolicy">IClient授权验证策略</param>
        /// <returns></returns>
        public static ICYarpAppBuilder RequireAuthorization(this ICYarpAppBuilder builder, AuthorizationPolicy clientPolicy)
        {
            builder.Authorization().AddPolicy(clientPolicy);
            return builder;
        }

        /// <summary>
        /// 添加IClient的授权验证策略名
        /// CYarpOptions.Authorization规则不再生效
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clientPolicyName">IClient授权验证策略名</param>
        /// <returns></returns>
        public static ICYarpAppBuilder RequireAuthorization(this ICYarpAppBuilder builder, string clientPolicyName)
        {
            builder.Authorization().AddPolicy(clientPolicyName);
            return builder;
        }

        /// <summary>
        /// 添加IClient授权验证策略配置
        /// CYarpOptions.Authorization规则不再生效
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureClientPolicy">IClient授权验证策略配置</param>
        /// <returns></returns>
        public static ICYarpAppBuilder RequireAuthorization(this ICYarpAppBuilder builder, Action<AuthorizationPolicyBuilder> configureClientPolicy)
        {
            builder.Authorization().AddPolicy(configureClientPolicy);
            return builder;
        }

        /// <summary>
        /// 跳过IClient的授权验证
        /// CYarpOptions.Authorization规则不再生效
        /// </summary>
        /// <param name="builder"></param> 
        public static void AllowAnonymous(this ICYarpAppBuilder builder)
        {
            builder.Authorization().SetAllowAnonymous();
        }

        private static ClientPolicyService Authorization(this ICYarpAppBuilder app)
        {
            return app.ApplicationServices.GetRequiredService<ClientPolicyService>();
        }

        private class CYarpAppBuilder : ICYarpAppBuilder
        {
            public IServiceProvider ApplicationServices { get; set; }

            public CYarpAppBuilder(IServiceProvider applicationServices)
            {
                this.ApplicationServices = applicationServices;
            }
        }
    }
}
