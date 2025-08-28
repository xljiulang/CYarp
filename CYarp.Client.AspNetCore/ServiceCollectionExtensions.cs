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
        /// Register CYarp listener
        /// To support kestrel listening on CYarp endpoints
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
