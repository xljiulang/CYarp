﻿using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// http隧道配置
    /// </summary>
    public class HttpTunnelConfig
    {
        /// <summary>
        /// 与目标服务器的最大连接数
        /// 默认为10
        /// </summary>
        public int MaxConnectionsPerServer { get; set; } = 10;

        /// <summary>
        /// 连接超时时长
        /// 默认10s
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 接受不安装的服务器证书
        /// 默认为true
        /// </summary>
        public bool DangerousAcceptAnyServerCertificate { get; set; } = true;
    }
}