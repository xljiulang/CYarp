using CYarp.Server.Configs;
using Microsoft.AspNetCore.Connections;
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
    sealed class ClientConnection : IAsyncDisposable
    {
        private int serialNumber = 0;
        private readonly Stream stream;
        private readonly ILogger<Client> logger;
        private readonly Timer? keepAliveTimer;
        private readonly TimeSpan keepAliveTimeout;
        private readonly CancellationTokenSource disposeTokenSource = new();

        private static readonly int bufferSize = 8;
        private const string Ping = "PING";
        private const string Pong = "PONG";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        public string ClientId { get; }

        public ClientConnection(
            string clientId,
            Stream stream,
            ClientConfig config,
            ILogger<Client> logger)
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
                ClientLog.LogSendPing(this.logger, this.ClientId);
            }
            catch (Exception)
            {
                this.keepAliveTimer?.Dispose();
            }
        }

        public TunnelId NewTunnelId(TunnelType tunnelType)
        {
            var value = (short)Interlocked.Increment(ref this.serialNumber);
            return TunnelId.NewTunnelId(this.ClientId, tunnelType, value);
        }

        public async Task CreateTunnelAsync(TunnelId tunnelId, CancellationToken cancellationToken)
        {
            const int size = 64;
            var tunnelIdLine = $"{tunnelId}\r\n";

            using var owner = MemoryPool<byte>.Shared.Rent(size);
            var length = Encoding.ASCII.GetBytes(tunnelIdLine, owner.Memory.Span);

            var buffer = owner.Memory[..length];
            await this.stream.WriteAsync(buffer, cancellationToken);
        }


        public async Task<ClientCloseReason> WaitForCloseAsync()
        {
            var cancellationToken = this.disposeTokenSource.Token;
            try
            {
                return await this.HandleConnectionAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return cancellationToken.IsCancellationRequested
                    ? ClientCloseReason.ServerAborted
                    : ClientCloseReason.PingPongTimeout;
            } 
            catch (ConnectionResetException)
            {
                return ClientCloseReason.RemoteAborted;
            }
            catch (Exception)
            {
                return ClientCloseReason.ServerAborted;
            }
        }

        private async Task<ClientCloseReason> HandleConnectionAsync(CancellationToken cancellationToken)
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
                    return ClientCloseReason.RemoteAborted;
                }

                if (text == Ping)
                {
                    ClientLog.LogRecvPing(this.logger, this.ClientId);
                    await this.stream.WriteAsync(PongLine, cancellationToken);
                }
                else if (text == Pong)
                {
                    ClientLog.LogRecvPong(this.logger, this.ClientId);
                }
                else
                {
                    ClientLog.LogRecvUnknown(this.logger, this.ClientId, text);
                }
            }
            return ClientCloseReason.ServerAborted;
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
    }
}
