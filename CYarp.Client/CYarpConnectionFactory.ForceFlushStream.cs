using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed partial class CYarpConnectionFactory
    {
        /// <summary>
        /// 自动刷新的Stream
        /// </summary>
        private class ForceFlushStream : DelegatingStream
        {
            public ForceFlushStream(Stream inner)
                : base(inner)
            {
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
                throw new InvalidOperationException($"只能调用{nameof(DisposeAsync)}()方法");
            }
        }
    }
}
