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
            return result.MessageType == WebSocketMessageType.Close ? 0 : result.Count;
        }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override void Dispose(bool disposing)
        {
            this.webSocket.Dispose();
            base.Dispose(disposing);
        }
    }
}
