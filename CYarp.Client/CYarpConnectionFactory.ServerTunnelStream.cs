using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed partial class CYarpConnectionFactory
    {
        /// <summary>
        /// Auto-flushing stream with cancellation signaling
        /// </summary>
        public class ServerTunnelStream : DelegatingStream
        {
            private readonly Guid tunnelId;
            private readonly CancellationTokenSource cancellationTokenSource = new();
            
            public CancellationToken CancellationToken => cancellationTokenSource.Token;

            public ServerTunnelStream(Guid tunnelId, Stream inner)
                : base(inner)
            {
                this.tunnelId = tunnelId;
            }

            public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
            {
                await base.WriteAsync(source, cancellationToken);
                await this.FlushAsync(cancellationToken);
            }

            public override ValueTask DisposeAsync()
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                return this.Inner.DisposeAsync();
            }

            protected override void Dispose(bool disposing)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
                this.Inner.Dispose();
            }

            public override string ToString()
            {
                return this.tunnelId.ToString();
            }
        }
    }
}
