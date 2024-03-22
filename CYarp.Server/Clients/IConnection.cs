using System;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 定义长连接
    /// </summary>
    interface IConnection : IDisposable
    {
        /// <summary>
        /// 获取唯一标识
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 创建http隧道
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CreateHttpTunnelAsync(Guid tunnelId, CancellationToken cancellationToken);

        /// <summary>
        /// 等待直到关闭
        /// </summary>
        /// <returns></returns>
        Task WaitForCloseAsync();
    }
}
