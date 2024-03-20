using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;

namespace CYarp.Client
{
    /// <summary>
    /// 客户端选项
    /// </summary>
    public class CYarpClientOptions
    {
        private static readonly string[] httpSchemes = [Uri.UriSchemeHttp, Uri.UriSchemeHttps];

        /// <summary>
        /// CYarp服务器Uri
        /// 支持http和https
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// 目标服务器Uri
        /// 支持http、https
        /// </summary>
        [AllowNull]
        public Uri TargetUri { get; set; }

        /// <summary>
        /// 目标服务器的UnixDomainSocket路径[可选]
        /// </summary>
        public string? TargetUnixDomainSocket { get; set; }

        /// <summary>
        /// 连接到CYarp服务器的Authorization请求头的值
        /// </summary>
        [AllowNull]
        public string Authorization { get; set; }

        /// <summary>
        /// 与server或target的连接超时时长
        /// 默认为5s
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// 隧道传输错误回调
        /// </summary>
        public Action<Exception>? TunnelErrorCallback;

        /// <summary>
        /// 验证参数
        /// </summary>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void Validate()
        {
            if (this.TargetUri == null)
            {
                throw new ArgumentNullException(nameof(this.TargetUri));
            }

            if (httpSchemes.Contains(this.TargetUri.Scheme) == false)
            {
                throw new ArgumentException($"TargetUri scheme must be http or https", nameof(TargetUri));
            }

            if (this.ServerUri == null)
            {
                throw new ArgumentNullException(nameof(ServerUri));
            }

            if (httpSchemes.Contains(this.ServerUri.Scheme) == false)
            {
                throw new ArgumentException("ServerUri scheme must be http or https", nameof(ServerUri));
            }

            if (string.IsNullOrEmpty(Authorization))
            {
                throw new ArgumentNullException(nameof(Authorization));
            }

            if (AuthenticationHeaderValue.TryParse(this.Authorization, out _) == false)
            {
                throw new ArgumentException("Authorization format is incorrect", nameof(Authorization));
            }

            if (this.ConnectTimeout <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(ConnectTimeout));
            }
        }
    }
}
