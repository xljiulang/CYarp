using System.Collections.Concurrent;

namespace CYarpGateway
{
    /// <summary>
    /// 网关选项
    /// </summary>
    sealed class GatewayOptions
    {
        /// <summary>
        /// 节点的Uri地址
        /// </summary>
        public ConcurrentDictionary<string, string> Nodes { get; set; } = [];
    }
}
