using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// 信号隧道
    /// </summary>
    sealed class SignalTunnel : DelegatingStream
    {
        private readonly SemaphoreSlim semaphoreSlim = new(1, 1);
        private readonly bool ownsInner;

        public SignalTunnel(Stream inner, bool ownsInner = true)
            : base(inner)
        {
            this.ownsInner = ownsInner;
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
            if (this.ownsInner)
            {
                this.Inner.Dispose();
            }
        }
    }
}
