using System;
using System.Threading;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// 连接配置
    /// </summary>
    public class ConnectionConfig
    {
        private static readonly TimeSpan delay = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// 心跳包周期
        /// 默认10s
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// 获取等待超时时长
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTimeout()
        {
            return this.KeepAliveInterval <= TimeSpan.Zero
                ? Timeout.InfiniteTimeSpan
                : this.KeepAliveInterval.Add(delay);
        }
    }
}
