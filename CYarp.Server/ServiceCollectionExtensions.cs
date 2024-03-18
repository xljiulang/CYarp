using CYarp.Server;
using CYarp.Server.Clients;
using CYarp.Server.Middlewares;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册CYarp相关服务
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCYarp(this IServiceCollection services)
        {
            services.TryAddSingleton<TunnelStreamFactory>();
            services.TryAddSingleton<IClientManager, ClientManager>();

            services.TryAddSingleton<CYarpMiddleware>();
            services.TryAddSingleton<CYarpClientMiddleware>();
            services.TryAddSingleton<TunnelStreamMiddleware>();
            services.TryAddSingleton<HttpForwardMiddleware>();

            return services.AddHttpForwarder();
        }

        /// <summary>
        /// 注册CYarp相关服务
        /// 配置CYarp选项
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configureOptions"></param>
        /// <returns></returns>
        public static IServiceCollection AddCYarp(this IServiceCollection services, Action<CYarpOptions> configureOptions)
        {
            return services.AddCYarp().Configure(configureOptions);
        }
    }
}
