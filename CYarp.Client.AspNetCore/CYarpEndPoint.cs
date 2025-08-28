using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore
{
    /// <summary>
    /// Cyarp终结点
    /// </summary>
    public sealed class CYarpEndPoint : EndPoint
    {
        /// <summary>
        /// GetOrSetCYarpServerUri
        /// 支持http、https、 wsAndwss
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// GetOrSet访问TargetServer（即本服务）UseUri
        /// 支持httpAndhttps
        /// </summary>
        public Uri TargetUri { get; set; } = new Uri("http://localhost");

        /// <summary>
        /// GetOrSetConnectionToCYarpServerRequest头
        /// </summary>
        public Dictionary<string, string> ConnectHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// GetOrSetConnectionToCYarpServerRequest头Factory
        /// 当ConnectHeadersFactory不Asnull时，ConnectHeadersWill会By忽略
        /// </summary>
        public Func<ValueTask<Dictionary<string, string>>>? ConnectHeadersFactory { get; set; } = null;

        /// <summary>
        /// GetOrSetAndCYarpServerConnectionTimeout时长
        /// 默认As5s
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// GetOrSetConnection心跳包周期
        /// 默认30s，小于等于0表示不发送心跳包
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// GetOrSet断线重连间隔间隔
        /// 默认5s
        /// </summary>
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// 转换As文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.TargetUri.Authority} <- {this.ServerUri}";
        }
    }
}
