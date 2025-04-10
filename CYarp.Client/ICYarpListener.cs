using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp监听器
    /// </summary>
    public interface ICYarpListener : IAsyncDisposable
    {
        /// <summary>
        /// 接收CYarp服务器的传输连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Stream?> AcceptAsync(CancellationToken cancellationToken = default);
    }
}
