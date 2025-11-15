using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    /// <summary>
    /// CYarp feature interface that defines CYarp request related functionality.
    /// </summary>
    public interface ICYarpFeature
    {
        /// <summary>
        /// Gets whether the current request is a CYarp request.
        /// </summary>
        bool IsCYarpRequest { get; }

        /// <summary>
        /// Gets whether the current request has been accepted.
        /// </summary>
        bool HasAccepted { get; }

        /// <summary>
        /// Gets the transport protocol of the current request.
        /// </summary>
        TransportProtocol Protocol { get; }

        /// <summary>
        /// Create a new duplex stream using the current physical connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result is the new duplex <see cref="Stream"/>.</returns>
        Task<Stream> AcceptAsStreamAsync();

        /// <summary>
        /// Create a new duplex stream with thread-safe write using the current physical connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation. The task result is the new thread-safe write duplex <see cref="Stream"/>.</returns>
        Task<Stream> AcceptAsSafeWriteStreamAsync();
    }
}
