using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp connection
    /// </summary>
    sealed partial class CYarpConnection : IAsyncDisposable
    {
        private readonly Stream stream;
        private readonly ILogger logger;
        private readonly StreamReader streamReader;
        private readonly Timer? keepAliveTimer;
        private readonly TimeSpan keepAliveTimeout;
        private readonly CancellationTokenSource closedTokenSource;

        private static readonly string Ping = "PING";
        private static readonly string Pong = "PONG";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        /// <summary>
        /// Gets the closed token
        /// </summary>
        public CancellationToken Closed { get; }

        public CYarpConnection(Stream stream, TimeSpan keepAliveInterval, ILogger logger)
        {
            this.stream = stream;
            this.logger = logger;
            this.streamReader = new StreamReader(stream, leaveOpen: true);

            if (keepAliveInterval > TimeSpan.Zero)
            {
                this.keepAliveTimeout = keepAliveInterval.Add(TimeSpan.FromSeconds(10d));
                this.keepAliveTimer = new Timer(this.KeepAliveTimerTick, null, keepAliveInterval, keepAliveInterval);
            }
            else
            {
                this.keepAliveTimeout = Timeout.InfiniteTimeSpan;
            }

            this.closedTokenSource = new CancellationTokenSource();
            this.Closed = this.closedTokenSource.Token;
        }

        /// <summary>
        /// Keep-alive timer
        /// </summary>
        /// <param name="state"></param>
        private async void KeepAliveTimerTick(object? state)
        {
            try
            {
                Log.LogSendPing(this.logger);
                await this.stream.WriteAsync(PingLine);
            }
            catch (Exception)
            {
                this.keepAliveTimer?.Dispose();
            }
        }

        /// <summary>
        /// Read tunnel id
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Guid?> ReadTunnelIdAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await this.ReadTunnelIdCoreAsync(cancellationToken);
            }
            catch (Exception)
            {
                cancellationToken.ThrowIfCancellationRequested();
                this.closedTokenSource.Cancel();
                return null;
            }
        }


        private async Task<Guid?> ReadTunnelIdCoreAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var textTask = this.streamReader.ReadLineAsync(cancellationToken);
                var text = this.keepAliveTimeout <= TimeSpan.Zero
                    ? await textTask
                    : await textTask.AsTask().WaitAsync(this.keepAliveTimeout, cancellationToken);

                if (text == null)
                {
                    this.closedTokenSource.Cancel();
                    return null;
                }

                if (text == Ping)
                {
                    Log.LogRecvPing(this.logger);
                    await this.stream.WriteAsync(PongLine, cancellationToken);
                }
                else if (text == Pong)
                {
                    Log.LogRecvPong(this.logger);
                }
                else if (Guid.TryParse(text, out var tunnelId))
                {
                    return tunnelId;
                }
                else
                {
                    Log.LogRecvUnknown(this.logger, text);
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            this.closedTokenSource.Cancel();
            this.closedTokenSource.Dispose();

            this.keepAliveTimer?.Dispose();
            this.streamReader.Dispose();
            return this.stream.DisposeAsync();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Debug, "Sent PING request")]
            public static partial void LogSendPing(ILogger logger);

            [LoggerMessage(LogLevel.Debug, "Received PING request")]
            public static partial void LogRecvPing(ILogger logger);

            [LoggerMessage(LogLevel.Debug, "Received PONG response")]
            public static partial void LogRecvPong(ILogger logger);

            [LoggerMessage(LogLevel.Debug, "Received unknown data: {text}")]
            public static partial void LogRecvUnknown(ILogger logger, string text);
        }
    }
}
