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
    /// Service registration extension methods
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Register CYarp related services.
        /// Provides an IClientViewer service to view IClient instances.
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
        /// Configure CYarpOptions
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureOptions">Configuration action for CYarpOptions</param>
        /// <returns></returns>
        public static ICYarpBuilder Configure(this ICYarpBuilder builder, Action<CYarpOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder;
        }

        /// <summary>
        /// Configure binding for CYarpOptions
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configureBinder">Configuration binder for CYarpOptions</param>
        /// <returns></returns>
        public static ICYarpBuilder Configure(this ICYarpBuilder builder, IConfiguration configureBinder)
        {
            builder.Services.Configure<CYarpOptions>(configureBinder);
            return builder;
        }

        /// <summary>
        /// Add a state storage for IClient
        /// </summary>
        /// <typeparam name="TStorage">The type of the state storage</typeparam>
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
