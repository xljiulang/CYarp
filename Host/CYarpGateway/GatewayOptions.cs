using System.Collections.Concurrent;

namespace CYarpGateway
{
    /// <summary>
    /// Gateway options
    /// </summary>
    sealed class GatewayOptions
    {
        /// <summary>
        /// Node URI addresses
        /// </summary>
        public ConcurrentDictionary<string, string> Nodes { get; set; } = [];
    }
}
