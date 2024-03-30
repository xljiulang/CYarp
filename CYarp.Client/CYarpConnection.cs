using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp连接
    /// </summary>
    sealed partial class CYarpConnection : IAsyncDisposable
    {
        private readonly Stream stream;
        private readonly ILogger logger;
        private readonly Timer? keepAliveTimer;
        private readonly TimeSpan keepAliveTimeout;

        private static readonly string Ping = "PING";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        public CYarpConnection(Stream stream, TimeSpan keepAliveInterval, ILogger logger)
        {
            this.stream = stream;
            this.logger = logger;

            if (keepAliveInterval > TimeSpan.Zero)
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
                Log.LogPing(this.logger);
                await this.stream.WriteAsync(PingLine);
            }
            catch (Exception)
            {
                this.keepAliveTimer?.Dispose();
            }
        }

        public async IAsyncEnumerable<Guid> ReadTunnelIdAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var textReader = new StreamReader(this.stream, leaveOpen: true);
            while (cancellationToken.IsCancellationRequested == false)
            {
                var textTask = textReader.ReadLineAsync(cancellationToken);
                var text = this.keepAliveTimeout <= TimeSpan.Zero
                    ? await textTask
                    : await textTask.AsTask().WaitAsync(this.keepAliveTimeout, cancellationToken);

                if (text == null)
                {
                    yield break;
                }
                else if (text == Ping)
                {
                    Log.LogPong(this.logger);
                    await this.stream.WriteAsync(PongLine, cancellationToken);
                }
                else if (Guid.TryParse(text, out var tunnelId))
                {
                    yield return tunnelId;
                }
            }
        }

        public ValueTask DisposeAsync()
        {
            this.keepAliveTimer?.Dispose();
            return this.stream.DisposeAsync();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Debug, "发出PING心跳")]
            public static partial void LogPing(ILogger logger);

            [LoggerMessage(LogLevel.Debug, "回复PONG心跳")]
            public static partial void LogPong(ILogger logger);
        }
    }
}
