using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed partial class CYarpConnectionFactory
    {
        /// <summary>
        /// 自动刷新Stream
        /// </summary>
        private class ServerTunnelStream : DelegatingStream
        {
            private readonly Guid tunnelId;

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
                return this.Inner.DisposeAsync();
            }

            protected override void Dispose(bool disposing)
            {
                this.Inner.Dispose();
            }

            public override string ToString()
            {
                return this.tunnelId.ToString();
            }
        }
    }
}
