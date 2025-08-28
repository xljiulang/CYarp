using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// ClientConfiguration
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// Gets or sets whether KeepAlive functionality is enabled
        /// Default is true
        /// </summary>
        public bool KeepAlive { get; set; } = true;

        /// <summary>
        /// Gets or sets heartbeat interval
        /// Default is 50 seconds
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(50d);
    }
}
