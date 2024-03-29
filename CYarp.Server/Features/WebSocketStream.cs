using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Features
{
    sealed class WebSocketStream : Stream
    {
        private readonly WebSocket webSocket;

        public WebSocketStream(WebSocket webSocket)
        {
            this.webSocket = webSocket;
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            return this.webSocket.SendAsync(buffer, WebSocketMessageType.Binary, endOfMessage: false, cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var result = await this.webSocket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                return 0;
            }
            else
            {
                return result.Count;
            }
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// ClientConnection会调用到此方法
        /// </summary>
        /// <returns></returns>
        public override async ValueTask DisposeAsync()
        {
            await this.CloseAndDisposeAsync();
        }

        /// <summary>
        /// ClientHttpHandler会调用到此方法
        /// 即SocketsHttpHandler检测到连接空闲时超时或捕获到其它异常
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            _ = CloseAndDisposeAsync();
        }

        private async Task CloseAndDisposeAsync()
        {
            if (this.webSocket.State == WebSocketState.Open)
            {
                using var timeoutTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(1d));
                await this.webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, null, timeoutTokenSource.Token).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            }
            this.webSocket.Dispose();
        }
    }
}
