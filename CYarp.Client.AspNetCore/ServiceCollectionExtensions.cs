using CYarp.Client.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 服务注册Extension
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册CYarpListener
        /// 以支持kestrel监听CYarpEndPoint
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCYarpListener(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IConnectionListenerFactory, CYarpConnectionListenerFactory>();
            return services;
        }
    }
}
