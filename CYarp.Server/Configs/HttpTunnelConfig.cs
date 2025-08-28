using System;
using System.Diagnostics;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// httpTunnelConfiguration
    /// </summary>
    public class HttpTunnelConfig
    {
        /// <summary>
        /// Gets or sets tunnel lifetime, tunnel will close after completing requests upon expiration
        /// Default is 10 minutes, must be greater than 0 seconds
        /// </summary>
        public TimeSpan LifeTime { get; set; } = TimeSpan.FromMinutes(10d);

        /// <summary>
        /// Gets or sets tunnel idle timeout duration, tunnel will close after idle timeout
        /// Default is 1 minute, must be greater than 0 seconds
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// Gets or sets maximum HTTP tunnel count per client
        /// Default is 10
        /// </summary>
        public int MaxTunnelsPerClient { get; set; } = 10;

        /// <summary>
        /// Gets or sets tunnel creation timeout duration
        /// Default is 10 seconds
        /// </summary>
        public TimeSpan CreationTimeout { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// Gets or sets whether to accept unsafe target server certificates
        /// Default is true
        /// </summary>
        public bool DangerousAcceptAnyServerCertificate { get; set; } = true;

        /// <summary>
        /// Gets or sets the <see cref="DistributedContextPropagator"/> to use when propagating distributed tracing and context
        /// </summary>
        public DistributedContextPropagator? ActivityHeadersPropagator { get; set; }
    }
}
