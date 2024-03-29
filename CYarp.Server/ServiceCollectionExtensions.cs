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
    /// 服务注册扩展
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

            services.TryAddSingleton<HttpTunnelFactory>();
            services.TryAddSingleton<ClientPolicyService>();
            services.TryAddSingleton<IClientIdProvider, DefaultClientIdProvider>();

            services.TryAddSingleton<CYarpMiddleware>();
            services.TryAddSingleton<ClientMiddleware>();
            services.TryAddSingleton<HttpTunnelMiddleware>();

            services.AddHttpForwarder();
            services.AddAuthorization();
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

        /// <summary>
        /// 添加一个IClient的状态存储器
        /// </summary>
        /// <typeparam name="TStorage">状态存储器的类型</typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ICYarpBuilder AddClientStateStorage<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TStorage>(this ICYarpBuilder builder)
            where TStorage : class, IClientStateStorage
        {
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IClientStateStorage, TStorage>());
            return builder;
        }

        /// <summary>
        /// 添加IClient的Id提供者
        /// </summary>
        /// <typeparam name="TProvider">IClient的Id提供者类型</typeparam>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ICYarpBuilder AddClientIdProvider<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TProvider>(this ICYarpBuilder builder)
           where TProvider : class, IClientIdProvider
        {
            builder.Services.Replace(ServiceDescriptor.Singleton<IClientIdProvider, TProvider>());
            return builder;
        }        

        private class CYarpBuilder(IServiceCollection services) : ICYarpBuilder
        {
            public IServiceCollection Services { get; } = services;
        }
    }
}
