using System.Threading;

namespace CYarp.Client
{
    /// <summary>
    /// Interface for streams that support cancellation signaling
    /// </summary>
    public interface ICancellableStream
    {
        /// <summary>
        /// Cancellation token that signals when this stream is cancelled/disposed
        /// </summary>
        CancellationToken CancellationToken { get; }

        /// <summary>
        /// Cancel this stream
        /// </summary>
        void Cancel();
    }
}
