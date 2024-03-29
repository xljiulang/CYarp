using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed class SafeWriteStream : DelegatingStream
    {
        private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
        private readonly bool ownsInner;

        public SafeWriteStream(Stream inner, bool ownsInner = true)
            : base(inner)
        {
            this.ownsInner = ownsInner;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            try
            {
                await semaphoreSlim.WaitAsync(CancellationToken.None);
                await base.WriteAsync(source, cancellationToken);
                await FlushAsync(cancellationToken);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            semaphoreSlim.Dispose();
            if (ownsInner)
            {
                Inner.Dispose();
            }
        }
    }
}
