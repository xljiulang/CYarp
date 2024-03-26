using Microsoft.Extensions.DependencyInjection;

namespace CYarp.Server
{
    /// <summary>
    /// CYarp的创建器
    /// </summary>
    public interface ICYarpBuilder
    {
        /// <summary>
        /// 获取服务集合
        /// </summary>
        IServiceCollection Services { get; }
    }
}
