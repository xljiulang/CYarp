using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore
{
    /// <summary>
    /// CYarp endpoint
    /// </summary>
    public sealed class CYarpEndPoint : EndPoint
    {
        /// <summary>
        /// Gets or sets CYarp server URI
        /// Supports http, https, ws and wss
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// Gets or sets URI for accessing target server (this service)
        /// Supports http and https
        /// </summary>
        public Uri TargetUri { get; set; } = new Uri("http://localhost");

        /// <summary>
        /// Gets or sets request headers for connecting to CYarp server
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
