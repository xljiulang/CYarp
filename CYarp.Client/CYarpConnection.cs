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
    sealed class CYarpConnection : IDisposable
    {
        private readonly Connection connection;
        private readonly TimeSpan timeout;
        private readonly Timer? keepAliveTimer;
        private readonly CancellationTokenSource disposeTokenSource = new();

        private static readonly string Ping = "PING";
        private static readonly ReadOnlyMemory<byte> PingLine = "PING\r\n"u8.ToArray();
        private static readonly ReadOnlyMemory<byte> PongLine = "PONG\r\n"u8.ToArray();

        public CYarpConnection(Stream stream, TimeSpan keepAliveInterval)
        {
            this.connection = new Connection(stream);
            this.timeout = keepAliveInterval > TimeSpan.Zero
                ? keepAliveInterval.Add(TimeSpan.FromSeconds(5d))
                : Timeout.InfiniteTimeSpan;

            if (keepAliveInterval > TimeSpan.Zero)
            {
                this.keepAliveTimer = new Timer(this.KeepAliveTimerTick, null, keepAliveInterval, keepAliveInterval);
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
            }
            catch (Exception)
            {
                this.keepAliveTimer?.Dispose();
            }
        }

        public async IAsyncEnumerable<Guid> ReadTunnelIdAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disposeTokenSource.Token);
            await foreach (var tunnelId in this.ReadTunnelIdCoreAsync(linkedTokenSource.Token))
            {
                yield return tunnelId;
            }
        }

        private async IAsyncEnumerable<Guid> ReadTunnelIdCoreAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var textReader = new StreamReader(connection, leaveOpen: true);
            while (cancellationToken.IsCancellationRequested == false)
            {
                using var timeoutTokenSource = new CancellationTokenSource(this.timeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);
                var text = await textReader.ReadLineAsync(linkedTokenSource.Token);

                if (text == null)
                {
                    yield break;
                }
                else if (text == Ping)
                {
                    await connection.WriteAsync(PongLine, cancellationToken);
                }
                else if (Guid.TryParse(text, out var tunnelId))
                {
                    yield return tunnelId;
                }
            }
        }

        public void Dispose()
        {
            this.disposeTokenSource.Cancel();
            this.disposeTokenSource.Dispose();
            this.connection.Dispose();
            this.keepAliveTimer?.Dispose();
        }


        private class Connection(Stream inner) : DelegatingStream(inner)
        {
            private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
            {
                try
                {
                    await this.semaphoreSlim.WaitAsync(CancellationToken.None);
                    await base.WriteAsync(source, cancellationToken);
                    await this.FlushAsync(cancellationToken);
                }
                finally
                {
                    this.semaphoreSlim.Release();
                }
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                this.semaphoreSlim.Dispose();
                this.Inner.Dispose();
            }
        }
    }
}
