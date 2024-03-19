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
        public Uri CYarpServer { get; set; }

        /// <summary>
        /// 目标服务器Uri
        /// </summary>
        [AllowNull]
        public Uri Destination { get; set; }

        /// <summary>
        /// 连接到CYarp服务器的Authorization请求头
        /// </summary>
        public string Authorization { get; set; } = string.Empty;


        public void Validate()
        {
            if (this.Destination == null)
            {
                throw new ArgumentNullException(nameof(this.Destination));
            }

            if (this.Destination.Scheme != Uri.UriSchemeHttp && this.Destination.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Uri scheme must be http or https", nameof(Destination));
            }

            if (this.CYarpServer == null)
            {
                throw new ArgumentNullException(nameof(CYarpServer));
            }

            if (this.CYarpServer.Scheme != Uri.UriSchemeHttp && this.CYarpServer.Scheme != Uri.UriSchemeHttps)
            {
                throw new ArgumentException("Uri scheme must be http or https", nameof(CYarpServer));
            }

            if (string.IsNullOrEmpty(Authorization))
            {
                throw new ArgumentNullException(nameof(Authorization));
            }
        }
    }
}
