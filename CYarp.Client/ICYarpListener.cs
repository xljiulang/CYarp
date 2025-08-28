using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarpListener
    /// </summary>
    public interface ICYarpListener : IAsyncDisposable
    {
        /// <summary>
        /// 接收CYarpServerTransportConnection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>返回null表示再也无法接Receive</returns>
        Task<Stream?> AcceptAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 接收CYarpServer所有TransportConnection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<Stream> AcceptAllAsync(CancellationToken cancellationToken = default);
    }
}
