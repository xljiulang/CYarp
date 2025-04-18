using System;
using System.Diagnostics;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// http隧道配置
    /// </summary>
    public class HttpTunnelConfig
    {
        /// <summary>
        /// 获取或设置隧道的生命周期，到期时完成请求后就关闭
        /// 默认为10分钟，要求必须大于0秒
        /// </summary>
        public TimeSpan LifeTime { get; set; } = TimeSpan.FromMinutes(10d);

        /// <summary>
        /// 获取或设置隧道的空闲超时时长，空闲超时后将关闭隧道
        /// 默认为1分钟，要求必须大于0秒
        /// </summary>
        public TimeSpan IdleTimeout { get; set; } = TimeSpan.FromMinutes(1d);

        /// <summary>
        /// 获取或设置每个客户端的最大http隧道数量
        /// 默认为10
        /// </summary>
        public int MaxTunnelsPerClient { get; set; } = 10;

        /// <summary>
        /// 获取或设置隧道创建的超时时长
        /// 默认10s
        /// </summary>
        public TimeSpan CreationTimeout { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 获取或设置接受不安全的的目标服务器证书
        /// 默认为true
        /// </summary>
        public bool DangerousAcceptAnyServerCertificate { get; set; } = true;

        /// <summary>
        /// 获取或设置在传播分布式跟踪和上下文时使用的<see cref="DistributedContextPropagator"/>
        /// </summary>
        public DistributedContextPropagator? ActivityHeadersPropagator { get; set; }
    }
}
