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
        /// GetOrSetCYarpServerUri
        /// 支持http、https、 wsAndwss
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// GetOrSet访问TargetServerUseUri
        /// 支持httpAndhttps
        /// </summary>
        [AllowNull]
        public Uri TargetUri { get; set; }

        /// <summary>
        ///GetOrSetTargetServerUnixDomainSocket路径[可选]
        /// </summary>
        public string? TargetUnixDomainSocket { get; set; }

        /// <summary>
        /// GetOrSetConnectionToCYarpServerRequest头
        /// </summary>
        public Dictionary<string, string> ConnectHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// GetOrSetAndserverOrtargetConnectionTimeout时长
        /// 默认As5s
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
