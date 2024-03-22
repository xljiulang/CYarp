using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// http隧道
    /// </summary>
    sealed partial class HttpTunnel : DelegatingStream
    {
        private readonly TaskCompletionSource closeTaskCompletionSource = new();
        private readonly ILogger logger;

        public Task Closed => this.closeTaskCompletionSource.Task;

        /// <summary>
        /// 隧道标识
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// 传输协议
        /// </summary>
        public string Protocol { get; }

        public HttpTunnel(Stream inner, Guid tunnelId, string protocol, ILogger logger)
            : base(inner)
        {
            this.Id = tunnelId;
            this.Protocol = protocol;
            this.logger = logger;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.Inner.Dispose();
            this.closeTaskCompletionSource.TrySetResult();

            Log.LogTunnelClosed(this.logger, this.Protocol, this.Id);
        }


        public override string ToString()
        {
            return this.Id.ToString();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "{protocol}隧道{tunnelId}已关闭")]
            public static partial void LogTunnelClosed(ILogger logger, string protocol, Guid tunnelId);
        }
    }
}
