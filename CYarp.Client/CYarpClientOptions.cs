using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CYarp.Client
{
    /// <summary>
    /// 客户端选项
    /// </summary>
    public class CYarpClientOptions
    {
        private static readonly string[] targetSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];
        private static readonly string[] serverSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeWs, Uri.UriSchemeWss];

        /// <summary>
        /// 获取或设置CYarp服务器Uri
        /// 支持http、https、 ws和wss
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// 获取或设置访问目标服务器使用的Uri
        /// 支持http和https
        /// </summary>
        [AllowNull]
        public Uri TargetUri { get; set; }

        /// <summary>
        ///获取或设置目标服务器的UnixDomainSocket路径[可选]
        /// </summary>
        public string? TargetUnixDomainSocket { get; set; }

        /// <summary>
        /// 获取或设置连接到CYarp服务器的请求头
        /// </summary>
        public Dictionary<string, string> ConnectHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取或设置与server或target的连接超时时长
        /// 默认为5s
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 获取或设置隧道传输错误回调
        /// </summary>
        public Action<Exception>? TunnelErrorCallback;

        /// <summary>
        /// 获取或设置心跳包周期
        /// 默认30s，小于等于0表示不发送心跳包
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Validate()
        {
            ArgumentNullException.ThrowIfNull(TargetUri);
            ArgumentNullException.ThrowIfNull(ServerUri);
            ArgumentOutOfRangeException.ThrowIfLessThan(ConnectTimeout, TimeSpan.Zero);

            if (targetSchemes.Contains(this.TargetUri.Scheme) == false)
            {
                throw new ArgumentException($"Scheme must be {string.Join(", ", targetSchemes)}", nameof(TargetUri));
            }

            if (serverSchemes.Contains(this.ServerUri.Scheme) == false)
            {
                throw new ArgumentException($"Scheme must be {string.Join(", ", serverSchemes)}", nameof(ServerUri));
            }
        }
    }
}
