using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYary.Client
{
    sealed class CYarpClientStream : DelegatingStream
    {
        public CYarpClientStream(Stream inner)
            : base(inner)
        {
        }

        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(source, cancellationToken);
            await FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            this.Inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
