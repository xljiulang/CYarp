using Microsoft.AspNetCore.Http;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed partial class CYarpFeature
    {
        private class HttpStream : DelegatingStream
        {
            private readonly HttpContext context;

            public HttpStream(HttpContext context, Stream inner)
                : base(inner)
            {
                this.context = context;
            }

            public override async ValueTask DisposeAsync()
            {
                this.context.Abort();
                await this.Inner.DisposeAsync();
            }

            protected override void Dispose(bool disposing)
            {
                this.context.Abort();
                this.Inner.Dispose();
            }
        }
    }
}
