using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CYarp.Server
{
    /// <summary>
    /// Viewer for CYarp's IClient
    /// </summary>
    public interface IClientViewer : IReadOnlyCollection<IClient>
    {
        /// <summary>
        /// Try to get client instance using clientId
        /// </summary>
        /// <param name="clientId">Client ID</param>
        /// <param name="client">Client instance</param>
        /// <returns></returns>
        bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IClient client);
    }
}
