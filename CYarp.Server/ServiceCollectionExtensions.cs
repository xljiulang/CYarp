using CYarp.Server;
using CYarp.Server.Clients;
using CYarp.Server.Middlewares;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册CYarp相关服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ICYarpBuilder AddCYarp(this IServiceCollection services)
        {
            services.TryAddSingleton<HttpTunnelFactory>();
            services.TryAddSingleton<IClientManager, ClientManager>();

            services.TryAddSingleton<CYarpMiddleware>();
            services.TryAddSingleton<CYarpClientMiddleware>();
            services.TryAddSingleton<HttpTunnelMiddleware>();

            services.AddHttpForwarder();
            return new CYarpBuilder(services);
        }

        /// <summary>
        /// 配置CYarpOptions
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions">CYarpOptions的配置</param>
        /// <returns></returns>
        public static ICYarpBuilder Configure(this ICYarpBuilder builder, Action<CYarpOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder;
        }

        /// <summary>
        /// 配置CYarpOptions的绑定
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureBinder">CYarpOptions的配置绑定</param>
        /// <returns></returns>
        public static ICYarpBuilder Configure(this ICYarpBuilder builder, IConfiguration configureBinder)
        {
            builder.Services.Configure<CYarpOptions>(configureBinder);
            return builder;
        }


        private class CYarpBuilder(IServiceCollection services) : ICYarpBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
