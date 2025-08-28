using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CYarp.Client
{
    /// <summary>
    /// ClientOptions
    /// </summary>
    public class CYarpClientOptions
    {
        private static readonly string[] targetSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];
        private static readonly string[] serverSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps, Uri.UriSchemeWs, Uri.UriSchemeWss];

        /// <summary>
        /// Gets or sets CYarp server URI
        /// Supports http, https, ws and wss
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// Gets or sets URI for accessing target server
        /// Supports http and https
        /// </summary>
        [AllowNull]
        public Uri TargetUri { get; set; }

        /// <summary>
        /// Gets or sets target server Unix domain socket path [optional]
        /// </summary>
        public string? TargetUnixDomainSocket { get; set; }

        /// <summary>
        /// Gets or sets request headers for connecting to CYarp server
        /// </summary>
        public Dictionary<string, string> ConnectHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets connection timeout duration for server or target connection
        /// Default is 5 seconds
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// GetOrSetTunnelTransportError回调
        /// </summary>
        public Action<Exception>? TunnelErrorCallback;

        /// <summary>
        /// GetOrSet心跳包周期
        /// 默认30s，小于等于0表示不发送心跳包
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// Verify参数
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
