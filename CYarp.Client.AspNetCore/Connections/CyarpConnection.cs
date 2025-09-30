using Microsoft.AspNetCore.Connections.Features;
using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore.Connections
{
    sealed class CyarpConnection : Stream, IConnectionLifetimeFeature
    {
        /// <summary>
        /// 获取所包装的流对象
        /// </summary>
        private readonly Stream inner;

        /// <summary>
        /// 连接中止的取消标记源
        /// </summary>
        private readonly CancellationTokenSource closedTokenSource = new();

        /// <summary>
        /// 获取连接中止的取消标记
        /// </summary>
        public CancellationToken ConnectionClosed { get; set; }

        /// <summary>
        /// 委托流
        /// </summary>
        /// <param name="inner"></param>
        public CyarpConnection(Stream inner)
        {
            this.inner = inner;
            this.ConnectionClosed = this.closedTokenSource.Token;
        }

        /// <inheritdoc/>
        public override bool CanRead => inner.CanRead;

        /// <inheritdoc/>
        public override bool CanSeek => inner.CanSeek;

        /// <inheritdoc/>
        public override bool CanWrite => inner.CanWrite;

        /// <inheritdoc/>
        public override long Length => inner.Length;

        /// <inheritdoc/>
        public override bool CanTimeout => inner.CanTimeout;

        /// <inheritdoc/>
        public override int ReadTimeout
        {
            get => inner.ReadTimeout;
            set => inner.ReadTimeout = value;
        }

        /// <inheritdoc/>
        public override int WriteTimeout
        {
            get => inner.WriteTimeout;
            set => inner.WriteTimeout = value;
        }


        /// <inheritdoc/>
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        /// <inheritdoc/>
        public override void Flush()
        {
            inner.Flush();
        }

        /// <inheritdoc/>
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return inner.FlushAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return inner.Read(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override int Read(Span<byte> destination)
        {
            return inner.Read(destination);
        }

        /// <inheritdoc/>
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        public override async ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default)
        {
            try
            {
                var bytes = await inner.ReadAsync(destination, cancellationToken);
                if (bytes == 0)
                {
                    this.closedTokenSource.Cancel();
                }
                return bytes;
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                this.closedTokenSource.Cancel();
                throw;
            }
        }

        /// <inheritdoc/>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return inner.Seek(offset, origin);
        }

        /// <inheritdoc/>
        public override void SetLength(long value)
        {
            inner.SetLength(value);
        }

        /// <inheritdoc/>
        public override void Write(byte[] buffer, int offset, int count)
        {
            inner.Write(buffer, offset, count);
        }

        /// <inheritdoc/>
        public override void Write(ReadOnlySpan<byte> source)
        {
            inner.Write(source);
        }

        /// <inheritdoc/>
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return this.WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        /// <inheritdoc/>
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
        {
            try
            {
                await inner.WriteAsync(source, cancellationToken);
            }
            catch (IOException ex) when (ex.InnerException is SocketException)
            {
                this.closedTokenSource.Cancel();
                throw;
            }
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToAsyncResult.Begin(ReadAsync(buffer, offset, count), callback, state);
        }

        /// <inheritdoc/>
        public override int EndRead(IAsyncResult asyncResult)
        {
            return TaskToAsyncResult.End<int>(asyncResult);
        }

        /// <inheritdoc/>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            return TaskToAsyncResult.Begin(WriteAsync(buffer, offset, count), callback, state);
        }

        /// <inheritdoc/>
        public override void EndWrite(IAsyncResult asyncResult)
        {
            TaskToAsyncResult.End(asyncResult);
        }

        /// <inheritdoc/>
        public override int ReadByte()
        {
            return inner.ReadByte();
        }

        /// <inheritdoc/>
        public override void WriteByte(byte value)
        {
            inner.WriteByte(value);
        }

        protected override void Dispose(bool disposing)
        {
            this.closedTokenSource.Cancel();
            this.closedTokenSource.Dispose();

            this.inner.Dispose();
            base.Dispose(disposing);
        }

        public void Abort()
        {
            this.inner.Close();
        }
    }
}
