using CYarp.Server.Configs;

namespace CYarp.Server
{
    /// <summary>
    /// Options
    /// </summary>
    public class CYarpOptions
    {
        /// <summary>
        /// Gets or sets node name
        /// </summary>
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// Allowed transport protocols
        /// Default is All
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
