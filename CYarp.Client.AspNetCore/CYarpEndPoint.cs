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
        /// 获取或设置CYarp服务器Uri
        /// 支持http、https、 ws和wss
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// 获取或设置访问目标服务器（即本服务）使用的Uri
        /// 支持http和https
        /// </summary>
        [AllowNull]
        public Uri TargetUri { get; set; } = new Uri("http://localhost");

        /// <summary>
        /// 获取或设置连接到CYarp服务器的请求头
        /// </summary>
        public Dictionary<string, string> ConnectHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置连接到CYarp服务器的请求头的工厂
        /// 当ConnectHeadersFactory不为null时，ConnectHeaders将会被忽略
        /// </summary>
        public Func<ValueTask<Dictionary<string, string>>>? ConnectHeadersFactory { get; set; } = null;

        /// <summary>
        /// 获取或设置与CYarp服务器的连接超时时长
        /// 默认为5s
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 获取或设置连接心跳包周期
        /// 默认30s
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// 获取或设置断线重连间隔间隔
        /// 默认5s
        /// </summary>
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.TargetUri.Authority} <- {this.ServerUri}";
        }
    }
}
