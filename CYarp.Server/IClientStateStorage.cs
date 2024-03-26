using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// CYarp的IClient状态存储器
    /// </summary>
    public interface IClientStateStorage
    {
        /// <summary>
        /// 重置节点的客户端状态
        /// </summary>
        /// <param name="node">节点名称(CYarpOptions.Node)</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task ResetClientStatesAsync(string node, CancellationToken cancellationToken);

        /// <summary>
        /// 写入客户端状态
        /// </summary>
        /// <param name="clientState">客户端状态</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WriteClientStateAsync(ClientState clientState, CancellationToken cancellationToken);
    }
}
