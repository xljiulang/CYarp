using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// CYarp IClientState storage
    /// </summary>
    public interface IClientStateStorage
    {
        /// <summary>
        /// Initialize all clients as offline
        /// This method is called after service startup
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task InitClientStatesAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Write client state
        /// </summary>
        /// <param name="clientState">ClientState</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task WriteClientStateAsync(ClientState clientState, CancellationToken cancellationToken);
    }
}
