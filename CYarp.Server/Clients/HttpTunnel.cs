using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// http隧道
    /// </summary>
    sealed class HttpTunnel : DelegatingStream
    {
        private readonly TaskCompletionSource closeTaskCompletionSource = new();

        public Task Closed => this.closeTaskCompletionSource.Task;

        public Guid Id { get; }

        public HttpTunnel(Stream inner, Guid tunnelId)
            : base(inner)
        {
            this.Id = tunnelId;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            this.Inner.Dispose();
            this.closeTaskCompletionSource.TrySetResult();
        }


        public override string ToString()
        {
            return this.Id.ToString();
        }
    }
}
