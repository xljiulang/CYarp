using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// http隧道
    /// </summary>
    sealed class HttpTunnel : DelegatingStream
    {
        private readonly bool ownsInner;

        public HttpTunnel(Stream inner, bool ownsInner = true)
            : base(inner)
        {
            this.ownsInner = ownsInner;
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(source, cancellationToken);
            await this.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (this.ownsInner)
            {
                this.Inner.Dispose();
            }
        }
    }
}
