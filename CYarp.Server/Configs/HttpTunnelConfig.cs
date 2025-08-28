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
        /// GetOrSetTunnelLifetime，To期时CompletedRequest后就Close
        /// 默认As10分钟，要求必须大于0秒
        /// </summary>
        public TimeSpan LifeTime { get; set; } = TimeSpan.FromMinutes(10d);

        /// <summary>
        /// GetOrSetTunnel空闲Timeout时长，空闲Timeout后WillCloseTunnel
        /// 默认As1分钟，要求必须大于0秒
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// GetOrSet每个Client最大httpTunnelCount
        /// 默认As10
        /// </summary>
        public int MaxTunnelsPerClient { get; set; } = 10;

        /// <summary>
        /// GetOrSetTunnelCreateTimeout时长
        /// 默认10s
        /// </summary>
        public TimeSpan CreationTimeout { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// GetOrSet接受不安全TargetServer证书
        /// 默认Astrue
        /// </summary>
        public bool DangerousAcceptAnyServerCertificate { get; set; } = true;

        /// <summary>
        /// GetOrSet在传播分布式跟踪And上下文时Use<see cref="DistributedContextPropagator"/>
        /// </summary>
        public DistributedContextPropagator? ActivityHeadersPropagator { get; set; }
    }
}
