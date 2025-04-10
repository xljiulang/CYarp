using CYarp.Client.AspNetCore;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 服务注册扩展
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// 注册CYarp监听器
        /// 以支持kestrel监听CYarpEndPoint
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddCYarpListener(this IServiceCollection services)
        {
            services.AddLogging();
            services.AddSingleton<IConnectionListenerFactory, CYarpListenerFactory>();
            return services;
        }
    }
}
