using System;
using System.Threading;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// KeepAlive配置
    /// </summary>
    public class KeepAliveConfig
    {
        private TimeSpan? timeout;

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Enable { get; set; } = true;

        /// <summary>
        /// 心跳包周期
        /// 默认10s
        /// </summary>
        public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(10d);


        /// <summary>
        /// 获取超时时长
        /// </summary>
        /// <returns></returns>
        public TimeSpan GetTimeout()
        {
            if (this.timeout == null)
            {
                this.timeout = this.Enable == false || this.Interval <= TimeSpan.Zero
                   ? Timeout.InfiniteTimeSpan
                   : this.Interval.Add(TimeSpan.FromSeconds(5d));
            }
            return this.timeout.Value;
        }
    }
}
