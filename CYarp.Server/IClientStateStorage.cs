using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// CYarpIClientState存储器
    /// </summary>
    public interface IClientStateStorage
    {
        /// <summary>
        /// 初始化所有ClientAs离线
        /// 服务启动后触发此方法
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InitClientStatesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 写入ClientState
        /// </summary>
        /// <param name="clientState">ClientState</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WriteClientStateAsync(ClientState clientState, CancellationToken cancellationToken);
    }
}
