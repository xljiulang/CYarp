using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    sealed class TunnelStream : DelegatingStream
    {
        private readonly TaskCompletionSource closeTaskCompletionSource = new();

        public Task Closed => this.closeTaskCompletionSource.Task;

        public Guid Id { get; }

        public TunnelStream(Stream inner, Guid tunnelId)
            : base(inner)
        {
            this.Id = tunnelId;
        }

        protected override void Dispose(bool disposing)
        {
            this.Inner.Dispose();
            base.Dispose(disposing);

            this.closeTaskCompletionSource.TrySetResult();
        }


        public override string ToString()
        {
            return this.Id.ToString();
        }
    }
}
