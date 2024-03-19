using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// httpHandler配置
    /// </summary>
    public record HttpHandlerConfig
    {
        /// <summary>
        /// 与目标服务器的最大连接数
        /// 默认为10
        /// </summary>
        public int MaxConnectionsPerServer { get; init; } = 10;

        /// <summary>
        /// 连接超时时长
        /// 默认10s
        /// </summary>
        public TimeSpan ConnectTimeout { get; init; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 接受不安装的服务器证书
        /// 默认为true
        /// </summary>
        public bool DangerousAcceptAnyServerCertificate { get; init; } = true;
    }
}
