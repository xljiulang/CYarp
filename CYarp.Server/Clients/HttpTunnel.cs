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
        private ClientConnection? connection;
        private readonly ILogger logger;
        private readonly TaskCompletionSource closeTaskCompletionSource = new();

        /// <summary>
        /// 等待HttpClient对其关闭
        /// </summary>
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

        public void BindConnection(ClientConnection connection)
        {
            this.connection = connection;
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
                var httpTunnelCount = this.connection?.DecrementHttpTunnelCount();
                Log.LogTunnelClosed(this.logger, this.connection?.ClientId, this.Protocol, this.Id, httpTunnelCount);
            }
        }

        public override string ToString()
        {
            return this.Id.ToString();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] 关闭了{protocol}协议隧道{tunnelId}，当前隧道数为{tunnelCount}")]
            public static partial void LogTunnelClosed(ILogger logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, int? tunnelCount);
        }
    }
}
