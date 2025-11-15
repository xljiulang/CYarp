using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// Storage for IClient states used by CYarp
    /// </summary>
    public interface IClientStateStorage
    {
        /// <summary>
        /// Initialize all clients as offline
        /// This method is called after the service starts
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InitClientStatesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Write a client's state
        /// </summary>
        /// <param name="clientState">The client's state</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WriteClientStateAsync(ClientState clientState, CancellationToken cancellationToken);
    }
}
