using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    /// <summary>
    /// CYarp特征接口，定义CYarpRequest相关功能。
    /// </summary>
    public interface ICYarpFeature
    {
        /// <summary>
        /// GetCurrentRequestIs否AsCYarpRequest。
        /// </summary>
        bool IsCYarpRequest { get; }

        /// <summary>
        /// GetCurrentRequestIs否AlreadyBy接受。
        /// </summary>
        bool HasAccepted { get; }

        /// <summary>
        /// GetCurrentRequestTransportProtocol类型。
        /// </summary>
        TransportProtocol Protocol { get; }

        /// <summary>
        /// UseCurrent物理ConnectionCreate新双工Stream。
        /// </summary>
        /// <returns>表示Asynchronous操作任务，任务结果As新双工Stream <see cref="Stream"/>。</returns>
        Task<Stream> AcceptAsStreamAsync();

        /// <summary>
        /// UseCurrent物理ConnectionCreate新线程安全写入双工Stream。
        /// </summary>
        /// <returns>表示Asynchronous操作任务，任务结果As新线程安全写入双工Stream <see cref="Stream"/>。</returns>
        Task<Stream> AcceptAsSafeWriteStreamAsync();
    }
}
