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
        /// Gets or sets the CYarp server Uri
        /// Supports http, https, ws and wss
        /// </summary>
        [AllowNull]
        public Uri ServerUri { get; set; }

        /// <summary>
        /// Gets or sets the Uri used to access the target server (this service)
        /// Supports http and https
        /// </summary>
        public Uri TargetUri { get; set; } = new Uri("http://localhost");

        /// <summary>
        /// Gets or sets the headers used when connecting to CYarp server
        /// </summary>
        public Dictionary<string, string> ConnectHeaders { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets or sets a factory for connect headers
        /// When ConnectHeadersFactory is not null, ConnectHeaders will be ignored
        /// </summary>
        public Func<ValueTask<Dictionary<string, string>>>? ConnectHeadersFactory { get; set; } = null;

        /// <summary>
        /// Gets or sets the connection timeout to the CYarp server
        /// Default is 5s
        /// </summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Gets or sets the connection keep-alive interval
        /// Default is 30s, &lt;=0 means no keep-alive
        /// </summary>
        public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// Gets or sets the reconnect interval
        /// Default is 5s
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
