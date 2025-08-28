using Microsoft.Extensions.DependencyInjection;

namespace CYarp.Server
{
    /// <summary>
    /// CYarp builder
    /// </summary>
    public interface ICYarpBuilder
    {
        /// <summary>
        /// Gets service collection
        /// </summary>
        IServiceCollection Services { get; }
    }
}
