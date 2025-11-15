using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// Client configuration
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Gets or sets whether KeepAlive is enabled
        /// Default is true
        /// </summary>
        public bool KeepAlive { get; set; } = true;

        /// <summary>
        /// Gets or sets the heartbeat interval
        /// Default is 50s
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(50d);
    }
}
