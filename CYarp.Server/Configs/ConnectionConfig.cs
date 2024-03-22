using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// 连接配置
    /// </summary>
    public class ConnectionConfig
    {
        /// <summary>
        /// 是否启用 KeepAlive 功能
        /// 默认true
        /// </summary>
        public bool KeepAlive { get; set; } = true;

        /// <summary>
        /// 心跳包周期
        /// 默认50s
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(50d);
    }
}
