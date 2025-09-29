using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// Tunnel
    /// </summary>
    sealed partial class Tunnel : DelegatingStream
    {
        private readonly long tickCount = Environment.TickCount64;
        private readonly TaskCompletionSource disposeTaskCompletionSource = new();

        /// <summary>
        /// Tunnel identifier
        /// </summary>
        public TunnelId Id { get; }

        /// <summary>
        /// Transport protocol
        /// </summary>
        public TransportProtocol Protocol { get; }

        /// <summary>
        /// Get lifetime
        /// </summary>
        public TimeSpan Lifetime => TimeSpan.FromMilliseconds(Environment.TickCount64 - this.tickCount);

        /// <summary>
        /// Get or set dispose callback
        /// </summary>
        public Action<Tunnel>? DisposingCallback { get; set; }


        public Tunnel(Stream inner, TunnelId tunnelId, TransportProtocol protocol)
            : base(inner)
        {
            this.Id = tunnelId;
            this.Protocol = protocol;
        }

        public Task WaitForDisposeAsync()
        {
            return this.disposeTaskCompletionSource.Task;
        }

        public override ValueTask DisposeAsync()
        {
            this.SetDisposed();
            return this.Inner.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            this.SetDisposed();
            this.Inner.Dispose();
        }

        private void SetDisposed()
        {
            if (this.disposeTaskCompletionSource.TrySetResult())
            {
                this.DisposingCallback?.Invoke(this);
            }
        }

        public override string ToString()
        {
            return this.Id.ToString();
        }
    }
}
