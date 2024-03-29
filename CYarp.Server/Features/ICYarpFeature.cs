using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    /// <summary>
    /// CYarp特征
    /// </summary>
    interface ICYarpFeature
    {
        /// <summary>
        /// 获取当前是否为CYarp请求
        /// </summary>
        bool IsCYarpRequest { get; }

        /// <summary>
        /// 获取传输协议类型
        /// </summary>
        TransportProtocol Protocol { get; }

        /// <summary>
        /// 使用当前物理连接创建新的双工流
        /// </summary>
        /// <returns></returns>
        Task<Stream> AcceptAsStreamAsync();

        /// <summary>
        /// 使用当前物理连接创建新的线程安全写入的双工流
        /// </summary>
        /// <returns></returns>
        Task<Stream> AcceptAsSafeWriteStreamAsync();
    }
}
