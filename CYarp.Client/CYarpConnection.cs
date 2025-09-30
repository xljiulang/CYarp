using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarpConnection
    /// </summary>
    sealed partial class CYarpConnection : IAsyncDisposable
    {
        private readonly Stream stream;
        private readonly ILogger logger;
        private readonly StreamReader streamReader;
        private readonly Timer? keepAliveTimer;
        private readonly TimeSpan keepAliveTimeout;
        private readonly CancellationTokenSource closedTokenSource;
        private readonly Dictionary<Guid, CancellationTokenSource> activeTunnels = new();

        private static readonly string Ping = "PING";
        private static readonly string Pong = "PONG";
        private static readonly string Abrt = "ABRT";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        /// <summary>
        /// Get close token
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
        /// Heartbeat timer
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
        /// Read tunnel ID
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

        /// <summary>
        /// Register a tunnel for potential cancellation
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationTokenSource"></param>
        public void RegisterTunnel(Guid tunnelId, CancellationTokenSource cancellationTokenSource)
        {
            lock (this.activeTunnels)
            {
                this.activeTunnels[tunnelId] = cancellationTokenSource;
            }
        }

        /// <summary>
        /// Unregister a tunnel
        /// </summary>
        /// <param name="tunnelId"></param>
        public void UnregisterTunnel(Guid tunnelId)
        {
            lock (this.activeTunnels)
            {
                this.activeTunnels.Remove(tunnelId);
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
                else if (text.StartsWith(Abrt) && text.Length > 5)
                {
                    // Handle ABRT message: "ABRT tunnelId"
                    var tunnelIdText = text.Substring(5);
                    if (Guid.TryParse(tunnelIdText, out var abortTunnelId))
                    {
                        lock (this.activeTunnels)
                        {
                            if (this.activeTunnels.TryGetValue(abortTunnelId, out var tunnelCancellation))
                            {
                                tunnelCancellation.Cancel();
                                this.activeTunnels.Remove(abortTunnelId);
                                Log.LogRecvAbort(this.logger, abortTunnelId);
                            }
                        }
                    }
                    else
                    {
                        Log.LogRecvUnknown(this.logger, text);
                    }
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

            // Cancel all active tunnels
            lock (this.activeTunnels)
            {
                foreach (var tunnelCancellation in this.activeTunnels.Values)
                {
                    tunnelCancellation.Cancel();
                    tunnelCancellation.Dispose();
                }
                this.activeTunnels.Clear();
            }

            this.keepAliveTimer?.Dispose();
            this.streamReader.Dispose();
            return this.stream.DisposeAsync();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Debug, "SendPINGRequest")]
            public static partial void LogSendPing(ILogger logger);

            [LoggerMessage(LogLevel.Debug, "ReceivePINGRequest")]
            public static partial void LogRecvPing(ILogger logger);

            [LoggerMessage(LogLevel.Debug, "ReceivePONGResponse")]
            public static partial void LogRecvPong(ILogger logger);

            [LoggerMessage(LogLevel.Debug, "ReceiveABRTRequest: {tunnelId}")]
            public static partial void LogRecvAbort(ILogger logger, Guid tunnelId);

            [LoggerMessage(LogLevel.Debug, "ReceiveUnknownData: {text}")]
            public static partial void LogRecvUnknown(ILogger logger, string text);
        }
    }
}
