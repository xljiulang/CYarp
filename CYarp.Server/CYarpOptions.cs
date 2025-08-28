using CYarp.Server.Configs;

namespace CYarp.Server
{
    /// <summary>
    /// Options
    /// </summary>
    public class CYarpOptions
    {
        /// <summary>
        /// GetOrSet节点名称
        /// </summary>
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// 允许TransportProtocol
        /// 默认AsAll
        /// </summary>
        public Protocols Protocols { get; set; } = Protocols.All;

        /// <summary>
        /// ClientConfiguration
        /// </summary>
        public ClientConfig Client { get; set; } = new ClientConfig();

        /// <summary>
        /// httpTunnelConfiguration
        /// </summary>
        public HttpTunnelConfig HttpTunnel { get; set; } = new HttpTunnelConfig();
    }
}
