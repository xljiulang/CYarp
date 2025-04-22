using System;
using System.IO;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 隧道
    /// </summary>
    sealed partial class Tunnel : DelegatingStream
    {
        private readonly long tickCount = Environment.TickCount64;
        private readonly TaskCompletionSource disposeTaskCompletionSource = new();

        /// <summary>
        /// 隧道标识
        /// </summary>
        public TunnelId Id { get; }

        /// <summary>
        /// 传输协议
        /// </summary>
        public TransportProtocol Protocol { get; }

        /// <summary>
        /// 获取生命周期
        /// </summary>
        public TimeSpan Lifetime => TimeSpan.FromMilliseconds(Environment.TickCount64 - this.tickCount);

        /// <summary>
        /// 获取或设置释放回调
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
