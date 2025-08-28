using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed partial class CYarpFeature
    {
        /// <summary>
        /// Safe write stream
        /// </summary>
        private class SafeWriteStream : DelegatingStream
        {
            private readonly SemaphoreSlim semaphoreSlim = new(1, 1);

            public SafeWriteStream(Stream inner)
                : base(inner)
            {
            }

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

            public override ValueTask DisposeAsync()
            {
                this.semaphoreSlim.Dispose();
                return this.Inner.DisposeAsync();
            }

            protected override void Dispose(bool disposing)
            {
                this.semaphoreSlim.Dispose();
                this.Inner.Dispose();
            }
        }
    }
}
