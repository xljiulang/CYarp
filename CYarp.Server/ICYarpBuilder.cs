using Microsoft.Extensions.DependencyInjection;

namespace CYarp.Server
{
    /// <summary>
    /// Builder for configuring CYarp
    /// </summary>
    public interface ICYarpBuilder
    {
        /// <summary>
        /// Gets the service collection
        /// </summary>
        IServiceCollection Services { get; }
    }
}
