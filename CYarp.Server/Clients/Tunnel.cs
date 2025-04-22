using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 隧道
    /// </summary>
    sealed partial class Tunnel : DelegatingStream
    {
        private ClientConnection? connection;
        private readonly ILogger logger;
        private readonly long tickCount = Environment.TickCount64;
        private readonly TaskCompletionSource disposeTaskCompletionSource = new();

        /// <summary>
        /// 隧道标识
        /// </summary>
        public TunnelId Id { get; }

        /// <summary>
        /// 传输协议
        /// </summary>
        public TransportProtocol Protocol { get; }

        public Tunnel(Stream inner, TunnelId tunnelId, TransportProtocol protocol, ILogger logger)
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

        public Task WaitForDisposeAsync()
        {
            return this.disposeTaskCompletionSource.Task;
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
            if (this.disposeTaskCompletionSource.TrySetResult())
            {
                var httpTunnelCount = this.connection?.DecrementHttpTunnelCount();
                var lifeTime = TimeSpan.FromMilliseconds(Environment.TickCount64 - this.tickCount);
                Log.LogTunnelClosed(this.logger, this.connection?.ClientId, this.Protocol, this.Id, lifeTime, httpTunnelCount);
            }
        }

        public override string ToString()
        {
            return this.Id.ToString();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] 关闭了{protocol}协议隧道{tunnelId}，生命周期为{lifeTime}，其当前隧道总数为{tunnelCount}")]
            public static partial void LogTunnelClosed(ILogger logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan lifeTime, int? tunnelCount);
        }
    }
}
