using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed class SafeWriteStream : DelegatingStream
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

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.semaphoreSlim.Dispose();
            this.Inner.Dispose();
        }
    }
}
