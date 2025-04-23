using CYarp.Server.Configs;
using Microsoft.Extensions.Logging;
using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 客户端的长连接
    /// </summary>
    sealed partial class ClientConnection : IAsyncDisposable
    {
        private long tunnelValue = 0L;
        private readonly Stream stream;
        private readonly ILogger logger;
        private readonly Timer? keepAliveTimer;
        private readonly TimeSpan keepAliveTimeout;
        private readonly CancellationTokenSource disposeTokenSource = new();

        private static readonly int bufferSize = 8;
        private const string Ping = "PING";
        private const string Pong = "PONG";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        public string ClientId { get; }

        public ClientConnection(string clientId, Stream stream, ClientConfig config, ILogger logger)
        {
            this.ClientId = clientId;
            this.stream = stream;
            this.logger = logger;

            var keepAliveInterval = config.KeepAliveInterval;
            if (config.KeepAlive && keepAliveInterval > TimeSpan.Zero)
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
                await this.stream.WriteAsync(PingLine);
                Log.LogSendPing(this.logger, this.ClientId);
            }
            catch (Exception)
            {
                this.keepAliveTimer?.Dispose();
            }
        }

        public TunnelId NewTunnelId()
        {
            var value = Interlocked.Increment(ref this.tunnelValue);
            return TunnelId.NewTunnelId(this.ClientId, value);
        }

        public async Task CreateHttpTunnelAsync(TunnelId tunnelId, CancellationToken cancellationToken)
        {
            const int size = 64;
            var tunnelIdLine = $"{tunnelId}\r\n";

            using var owner = MemoryPool<byte>.Shared.Rent(size);
            var length = Encoding.ASCII.GetBytes(tunnelIdLine, owner.Memory.Span);

            var buffer = owner.Memory[..length];
            await this.stream.WriteAsync(buffer, cancellationToken);
        }


        public async Task WaitForCloseAsync()
        {
            try
            {
                var cancellationToken = this.disposeTokenSource.Token;
                await this.HandleConnectionAsync(cancellationToken);
            }
            catch (Exception)
            {
            }
        }

        private async Task HandleConnectionAsync(CancellationToken cancellationToken)
        {
            using var textReader = new StreamReader(this.stream, bufferSize: bufferSize, leaveOpen: true);
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

                if (text == Ping)
                {
                    Log.LogRecvPing(this.logger, this.ClientId);
                    await this.stream.WriteAsync(PongLine, cancellationToken);
                }
                else if (text == Pong)
                {
                    Log.LogRecvPong(this.logger, this.ClientId);
                }
                else
                {
                    Log.LogRecvUnknown(this.logger, this.ClientId, text);
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.disposeTokenSource.IsCancellationRequested == false)
            {
                this.keepAliveTimer?.Dispose();

                this.disposeTokenSource.Cancel();
                this.disposeTokenSource.Dispose();

                await this.stream.DisposeAsync();
            }
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Debug, "[{clientId}] 发出PING请求")]
            public static partial void LogSendPing(ILogger logger, string clientId);

            [LoggerMessage(LogLevel.Debug, "[{clientId}] 收到PING请求")]
            public static partial void LogRecvPing(ILogger logger, string clientId);

            [LoggerMessage(LogLevel.Debug, "[{clientId}] 收到PONG回应")]
            public static partial void LogRecvPong(ILogger logger, string clientId);

            [LoggerMessage(LogLevel.Debug, "[{clientId}] 收到未知数据: {text}")]
            public static partial void LogRecvUnknown(ILogger logger, string clientId, string text);

            [LoggerMessage(LogLevel.Debug, "[{clientId}] 连接已关闭")]
            public static partial void LogClosed(ILogger logger, string clientId);
        }
    }
}
