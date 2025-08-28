using Microsoft.Extensions.DependencyInjection;

namespace CYarp.Server
{
    /// <summary>
    /// CYarpCreate器
    /// </summary>
    public interface ICYarpBuilder
    {
        /// <summary>
        /// Get服务集合
        /// </summary>
        IServiceCollection Services { get; }
    }
}
