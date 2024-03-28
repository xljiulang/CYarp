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
        /// 自动管理IClient的连接
        /// 需要放到app.UseAuthentication()之后
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IClientAuthorizationBuilder UseCYarp(this IApplicationBuilder app)
        {
            app.UseMiddleware<CYarpMiddleware>();
            app.UseMiddleware<CYarpClientMiddleware>();
            app.UseMiddleware<HttpTunnelMiddleware>();
            return new ClientAuthorizationBuilder(app.ApplicationServices);
        }

        /// <summary>
        /// 添加IClient的授权验证策略
        /// CYarpOptions.Authorization不再生效
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clientPolicy">IClient授权验证策略</param>
        /// <returns></returns>
        public static IClientAuthorizationBuilder RequireAuthorization(this IClientAuthorizationBuilder builder, AuthorizationPolicy clientPolicy)
        {
            builder.Authorization().AddPolicy(clientPolicy);
            return builder;
        }

        /// <summary>
        /// 添加IClient的授权验证策略名
        /// CYarpOptions.Authorization不再生效
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="clientPolicyName">IClient授权验证策略名</param>
        /// <returns></returns>
        public static IClientAuthorizationBuilder RequireAuthorization(this IClientAuthorizationBuilder builder, string clientPolicyName)
        {
            builder.Authorization().AddPolicy(clientPolicyName);
            return builder;
        }

        /// <summary>
        /// 添加IClient授权验证策略配置
        /// CYarpOptions.Authorization不再生效
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureClientPolicy">IClient授权验证策略配置</param>
        /// <returns></returns>
        public static IClientAuthorizationBuilder RequireAuthorization(this IClientAuthorizationBuilder builder, Action<AuthorizationPolicyBuilder> configureClientPolicy)
        {
            builder.Authorization().AddPolicy(configureClientPolicy);
            return builder;
        }

        /// <summary>
        /// 跳过IClient的授权验证
        /// CYarpOptions.Authorization不再生效
        /// </summary>
        /// <param name="builder"></param> 
        public static void AllowAnonymous(this IClientAuthorizationBuilder builder)
        {
            builder.Authorization().SetAllowAnonymous();
        }

        private static ClientAuthorization Authorization(this IClientAuthorizationBuilder app)
        {
            return app.ApplicationServices.GetRequiredService<ClientAuthorization>();
        }

        private class ClientAuthorizationBuilder : IClientAuthorizationBuilder
        {
            public IServiceProvider ApplicationServices { get; set; }

            public ClientAuthorizationBuilder(IServiceProvider applicationServices)
            {
                this.ApplicationServices = applicationServices;
            }
        }
    }
}
