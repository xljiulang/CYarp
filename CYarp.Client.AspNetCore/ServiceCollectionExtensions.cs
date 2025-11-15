using CYarp.Client.AspNetCore.Connections;
using Microsoft.AspNetCore.Connections;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Service registration extensions
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers the CYarp listener
        /// Enables Kestrel to listen on CYarpEndPoint
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
