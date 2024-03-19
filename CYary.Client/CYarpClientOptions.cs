using System;
using System.Diagnostics.CodeAnalysis;

namespace CYary.Client
{
    /// <summary>
    /// 客户端选项
    /// </summary>
    public class CYarpClientOptions
    {
        /// <summary>
        /// CYarp服务器Uri
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// 目标服务器Uri
        /// </summary>
        [AllowNull]
        public Uri TargetUri { get; set; }

        /// <summary>
        /// 连接到CYarp服务器的Authorization请求头
        /// </summary>
        public string Authorization { get; set; } = string.Empty;

        /// <summary>
        /// 与server或target的连接超时时长
        /// 默认为30s
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);


        public void Validate()
        {
            if (this.TargetUri == null)
            {
                throw new ArgumentNullException(nameof(this.TargetUri));
            }

            if (this.TargetUri.Scheme != Uri.UriSchemeHttp && this.TargetUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Uri scheme must be http or https", nameof(TargetUri));
            }

            if (this.ServerUri == null)
            {
                throw new ArgumentNullException(nameof(ServerUri));
            }

            if (this.ServerUri.Scheme != Uri.UriSchemeHttp && this.ServerUri.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Uri scheme must be http or https", nameof(ServerUri));
            }

            if (string.IsNullOrEmpty(Authorization))
            {
                throw new ArgumentNullException(nameof(Authorization));
            }
        }
    }
}
