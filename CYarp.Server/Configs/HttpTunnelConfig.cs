using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// http隧道配置
    /// </summary>
    public class HttpTunnelConfig
    {
        /// <summary>
        /// 每个客户端的最大http隧道数量
        /// 默认为10
        /// </summary>
        public int MaxTunnelsPerClient { get; set; } = 10;

        /// <summary>
        /// http隧道创建的超时时长
        /// 默认10s
        /// </summary>
        public TimeSpan CreationTimeout { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 接受不安全的的目标服务器证书
        /// 默认为true
        /// </summary>
        public bool DangerousAcceptAnyServerCertificate { get; set; } = true;
    }
}
