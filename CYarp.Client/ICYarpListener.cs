using System;
using System.Collections.Generic;
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
        /// <returns>返回null表示再也无法接收到</returns>
        Task<Stream?> AcceptAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 接收CYarp服务器的所有传输连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<Stream> AcceptAllAsync(CancellationToken cancellationToken = default);
    }
}
