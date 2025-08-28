using System.Collections.Concurrent;

namespace CYarpGateway
{
    /// <summary>
    /// 网关Options
    /// </summary>
    sealed class GatewayOptions
    {
        /// <summary>
        /// 节点UriAddress
        /// </summary>
        public ConcurrentDictionary<string, string> Nodes { get; set; } = [];
    }
}
