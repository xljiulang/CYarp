using Microsoft.Extensions.Logging;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// http隧道
    /// </summary>
    sealed partial class HttpTunnel : DelegatingStream
    {
        private readonly ILogger logger;
        private readonly TaskCompletionSource closeTaskCompletionSource = new();

        public Task Closed => this.closeTaskCompletionSource.Task;

        /// <summary>
        /// 隧道标识
        /// </summary>
        public TunnelId Id { get; }

        /// <summary>
        /// 传输协议
        /// </summary>
        public TransportProtocol Protocol { get; }

        public HttpTunnel(Stream inner, TunnelId tunnelId, TransportProtocol protocol, ILogger logger)
            : base(inner)
        {
            this.Id = tunnelId;
            this.Protocol = protocol;
            this.logger = logger;
        }

        public override ValueTask DisposeAsync()
        {
            this.SetClosedResult();
            return this.Inner.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            this.SetClosedResult();
            this.Inner.Dispose();
        }

        private void SetClosedResult()
        {
            if (this.closeTaskCompletionSource.TrySetResult())
            {
                Log.LogTunnelClosed(this.logger, this.Protocol, this.Id);
            }
        }


        public override string ToString()
        {
            return this.Id.ToString();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "{protocol}隧道{tunnelId}已关闭")]
            public static partial void LogTunnelClosed(ILogger logger, TransportProtocol protocol, TunnelId tunnelId);
        }
    }
}
