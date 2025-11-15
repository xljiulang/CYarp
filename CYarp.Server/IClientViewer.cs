using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CYarp.Server
{
    /// <summary>
    /// Viewer for IClient instances in CYarp
    /// </summary>
    public interface IClientViewer : IReadOnlyCollection<IClient>
    {
        /// <summary>
        /// Attempts to get a client instance by clientId
        /// </summary>
        /// <param name="clientId">The client id</param>
        /// <param name="client">The client instance</param>
        /// <returns>True if the client was found; otherwise false</returns>
        bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IClient client);
    }
}
