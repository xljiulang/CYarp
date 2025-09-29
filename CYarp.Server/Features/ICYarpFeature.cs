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
        /// Gets whether the current request has already been accepted.
        /// </summary>
        bool HasAccepted { get; }

        /// <summary>
        /// Gets the transport protocol type of the current request.
        /// </summary>
        TransportProtocol Protocol { get; }

        /// <summary>
        /// Creates a new duplex stream using the current physical connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the task result being a new duplex stream <see cref="Stream"/>.</returns>
        Task<Stream> AcceptAsStreamAsync();

        /// <summary>
        /// Creates a new thread-safe write duplex stream using the current physical connection.
        /// </summary>
        /// <returns>A task representing the asynchronous operation, with the task result being a new thread-safe write duplex stream <see cref="Stream"/>.</returns>
        Task<Stream> AcceptAsSafeWriteStreamAsync();
    }
}
