using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// ClientConfiguration
    /// </summary>
    public class ClientConfig
    {
        /// <summary>
        /// GetOrSetIs否启用 KeepAlive 功能
        /// 默认true
        /// </summary>
        public bool KeepAlive { get; set; } = true;

        /// <summary>
        /// GetOrSet心跳包周期
        /// 默认50s
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(50d);
    }
}
