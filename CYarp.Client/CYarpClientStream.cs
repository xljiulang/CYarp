using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed class CYarpClientStream : DelegatingStream
    {
        public Version Version { get; }

        public CYarpClientStream(Stream inner, Version version)
            : base(inner)
        {
            this.Version = version;
        }


        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            await base.WriteAsync(source, cancellationToken);
            await this.FlushAsync(cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.Inner.Dispose();
        }
    }
}
