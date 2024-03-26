using CYarp.Server.Clients;
using CYarp.Server.Configs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Middlewares
{
    [DebuggerDisplay("Id = {Id}, Protocol = {Protocol}")]
    sealed partial class CYarpClient : ClientBase
    {
        private readonly CYarpConnection connection;
        private readonly Timer? keepAliveTimer;
        private readonly TimeSpan keepAliveTimeout;
        private readonly ILogger logger;

        private static readonly int bufferSize = 8;
        private static readonly string Ping = "PING";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        public CYarpClient(
            CYarpConnection connection,
            ConnectionConfig connectionConfig,
            IHttpForwarder httpForwarder,
            HttpTunnelConfig httpTunnelConfig,
            HttpTunnelFactory httpTunnelFactory,
            string clientId,
            Uri clientDestination,
            HttpContext httpContext,
            ILogger logger) : base(httpForwarder, httpTunnelConfig, httpTunnelFactory, clientId, clientDestination, httpContext)
        {
            this.connection = connection;
            this.logger = logger;

            var keepAliveInterval = connectionConfig.KeepAliveInterval;
            if (connectionConfig.KeepAlive && keepAliveInterval > TimeSpan.Zero)
            {
                this.keepAliveTimeout = keepAliveInterval.Add(TimeSpan.FromSeconds(10d));
                this.keepAliveTimer = new Timer(this.KeepAliveTimerTick, null, keepAliveInterval, keepAliveInterval);
            }
            else
            {
                this.keepAliveTimeout = Timeout.InfiniteTimeSpan;
            }
        }

        /// <summary>
        /// 心跳timer
        /// </summary>
        /// <param name="state"></param>
        private async void KeepAliveTimerTick(object? state)
        {
            try
            {
                await this.connection.WriteAsync(PingLine);
                Log.LogPing(this.logger, this.Id);
            }
            catch (Exception)
            {
                this.keepAliveTimer?.Dispose();
            }
        }

        public override async Task CreateHttpTunnelAsync(Guid tunnelId, CancellationToken cancellationToken = default)
        {
            const int size = 64;
            var tunnelIdLine = $"{tunnelId}\r\n";

            using var owner = MemoryPool<byte>.Shared.Rent(size);
            var length = Encoding.ASCII.GetBytes(tunnelIdLine, owner.Memory.Span);

            var buffer = owner.Memory[..length];
            await this.connection.WriteAsync(buffer, cancellationToken);
        }

        protected override async Task HandleConnectionAsync(CancellationToken cancellationToken)
        {
            using var textReader = new StreamReader(this.connection, bufferSize: bufferSize, leaveOpen: true);
            while (cancellationToken.IsCancellationRequested == false)
            {
                var textTask = textReader.ReadLineAsync(cancellationToken);
                var text = this.keepAliveTimeout <= TimeSpan.Zero
                    ? await textTask
                    : await textTask.AsTask().WaitAsync(this.keepAliveTimeout, cancellationToken);

                if (text == null)
                {
                    break;
                }
                else if (text == Ping)
                {
                    Log.LogPong(this.logger, this.Id);
                    await this.connection.WriteAsync(PongLine, cancellationToken);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.connection.Dispose();
            this.keepAliveTimer?.Dispose();

            Log.LogClosed(this.logger, this.Id);
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Debug, "[{clienId}] 发出PING心跳")]
            public static partial void LogPing(ILogger logger, string clienId);

            [LoggerMessage(LogLevel.Debug, "[{clienId}] 回复PONG心跳")]
            public static partial void LogPong(ILogger logger, string clienId);

            [LoggerMessage(LogLevel.Debug, "[{clienId}] 连接已关闭")]
            public static partial void LogClosed(ILogger logger, string clienId);
        }
    }
}
