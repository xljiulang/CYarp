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
        /// Gets or sets request header factory for connecting to CYarp server
        /// When ConnectHeadersFactory is not null, ConnectHeaders will be ignored
        /// </summary>
        public Func<ValueTask<Dictionary<string, string>>>? ConnectHeadersFactory { get; set; } = null;

        /// <summary>
        /// Gets or sets connection timeout duration to CYarp server
        /// Default is 5 seconds
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets connection heartbeat interval
        /// Default is 30 seconds, less than or equal to 0 means no heartbeat packets are sent
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// Gets or sets reconnection interval when disconnected
        /// Default is 5 seconds
        /// </summary>
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// Convert to string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{this.TargetUri.Authority} <- {this.ServerUri}";
        }
    }
}
