using CYarp.Server;
using CYarp.Server.Clients;
using CYarp.Server.Middlewares;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 服务注册Extension
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册CYarp相关服务
        /// 提供IClientViewer服务来查看IClient
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static ICYarpBuilder AddCYarp(this IServiceCollection services)
        {
            services.TryAddSingleton<ClientStateChannel>();
            services.AddHostedService<ClientStateStorageService>();

            services.TryAddSingleton<ClientManager>();
            services.TryAddSingleton<IClientViewer>(serviceProvder => serviceProvder.GetRequiredService<ClientManager>());

            services.TryAddSingleton<TunnelFactory>();
            services.TryAddSingleton<CYarpMiddleware>();

            services.AddHttpForwarder();          
            return new CYarpBuilder(services);
        }

        /// <summary>
        /// ConfigurationCYarpOptions
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions">CYarpOptionsConfiguration</param>
        /// <returns></returns>
        public static ICYarpBuilder Configure(this ICYarpBuilder builder, Action<CYarpOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder;
        }

        /// <summary>
        /// ConfigurationCYarpOptions绑定
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureBinder">CYarpOptionsConfiguration绑定</param>
        /// <returns></returns>
        public static ICYarpBuilder Configure(this ICYarpBuilder builder, IConfiguration configureBinder)
        {
            builder.Services.Configure<CYarpOptions>(configureBinder);
            return builder;
        }

        /// <summary>
        /// 添加一个IClientState存储器
        /// </summary>
        /// <typeparam name="TStorage">State存储器类型</typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ICYarpBuilder AddClientStateStorage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStorage>(this ICYarpBuilder builder)
            where TStorage : class, IClientStateStorage
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientStateStorage, TStorage>());
            return builder;
        }


        private class CYarpBuilder(IServiceCollection services) : ICYarpBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
