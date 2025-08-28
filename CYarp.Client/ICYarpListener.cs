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
        /// Accept CYarp server transport connection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Returns null when no more connections can be accepted</returns>
        Task<Stream?> AcceptAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Accept all CYarp server transport connections
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IAsyncEnumerable<Stream> AcceptAllAsync(CancellationToken cancellationToken = default);
    }
}
