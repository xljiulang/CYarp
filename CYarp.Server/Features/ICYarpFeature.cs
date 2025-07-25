using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    /// <summary>
    /// CYarp特征接口，定义CYarp请求相关的功能。
    /// </summary>
    public interface ICYarpFeature
    {
        /// <summary>
        /// 获取当前请求是否为CYarp请求。
        /// </summary>
        bool IsCYarpRequest { get; }

        /// <summary>
        /// 获取当前请求是否已被接受。
        /// </summary>
        bool HasAccepted { get; }

        /// <summary>
        /// 获取当前请求的传输协议类型。
        /// </summary>
        TransportProtocol Protocol { get; }

        /// <summary>
        /// 使用当前物理连接创建新的双工流。
        /// </summary>
        /// <returns>表示异步操作的任务，任务结果为新的双工流 <see cref="Stream"/>。</returns>
        Task<Stream> AcceptAsStreamAsync();

        /// <summary>
        /// 使用当前物理连接创建新的线程安全写入的双工流。
        /// </summary>
        /// <returns>表示异步操作的任务，任务结果为新的线程安全写入的双工流 <see cref="Stream"/>。</returns>
        Task<Stream> AcceptAsSafeWriteStreamAsync();
    }
}
