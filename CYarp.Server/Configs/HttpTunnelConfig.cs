using System;
using System.Diagnostics;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// HTTP tunnel configuration
    /// </summary>
    public class HttpTunnelConfig
    {
        /// <summary>
        /// Gets or sets the tunnel lifetime. The tunnel will be closed after completing requests when expired.
        /// Default is 10 minutes, must be greater than 0 seconds.
        /// </summary>
        public TimeSpan LifeTime { get; set; } = TimeSpan.FromMinutes(10d);

        /// <summary>
        /// Gets or sets the idle timeout for the tunnel. The tunnel will be closed after idle timeout.
        /// Default is 1 minute, must be greater than 0 seconds.
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// Gets or sets the maximum number of HTTP tunnels per client
        /// Default is 10
        /// </summary>
        public int MaxTunnelsPerClient { get; set; } = 10;

        /// <summary>
        /// Gets or sets the timeout for tunnel creation
        /// Default is 10s
        /// </summary>
        public TimeSpan CreationTimeout { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// Gets or sets whether to accept insecure server certificates
        /// Default is true
        /// </summary>
        public bool DangerousAcceptAnyServerCertificate { get; set; } = true;

        /// <summary>
        /// Gets or sets the <see cref="DistributedContextPropagator"/> used when propagating distributed tracing and context
        /// </summary>
        public DistributedContextPropagator? ActivityHeadersPropagator { get; set; }
    }
}
