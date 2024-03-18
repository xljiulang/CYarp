using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    sealed class TunnelStream : DelegatingStream
    {
        private readonly TaskCompletionSource closeTaskCompletionSource = new();

        public Task Closed => closeTaskCompletionSource.Task;

        public Guid Id { get; }

        public TunnelStream(Stream inner, Guid tunnelId)
            : base(inner)
        {
            Id = tunnelId;
        }

        public override void Close()
        {
            Inner.Close();
            base.Close();
        }


        protected override void Dispose(bool disposing)
        {
            closeTaskCompletionSource.TrySetResult();
            base.Dispose(disposing);
        }


        public override string ToString()
        {
            return Id.ToString();
        }
    }
}
