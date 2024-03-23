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
        public static IServiceCollection AddCYarp(this IServiceCollection services)
        {
            services.TryAddSingleton<HttpTunnelFactory>();
            services.TryAddSingleton<IClientManager, ClientManager>();

            services.TryAddSingleton<CYarpMiddleware>();
            services.TryAddSingleton<CYarpClientMiddleware>();
            services.TryAddSingleton<HttpTunnelMiddleware>();

            return services.AddHttpForwarder();
        }

        /// <summary>
        /// 注册CYarp相关服务并配置CYarpOptions
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions">CYarpOptions的配置</param>
        /// <returns></returns>
        public static IServiceCollection AddCYarp(this IServiceCollection services, Action<CYarpOptions> configureOptions)
        {
            return services.AddCYarp().Configure(configureOptions);
        }

        /// <summary>
        /// 注册CYarp相关服务并配置CYarpOptions的绑定
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureBinder">CYarpOptions的配置绑定</param>
        /// <returns></returns>
        public static IServiceCollection AddCYarp(this IServiceCollection services, IConfiguration configureBinder)
        {
            return services.AddCYarp().Configure<CYarpOptions>(configureBinder);
        }
    }
}
