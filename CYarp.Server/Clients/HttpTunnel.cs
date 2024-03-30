using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Hashing;
using System.Text;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// http隧道
    /// </summary>
    sealed partial class HttpTunnel : DelegatingStream
    {
        private readonly ILogger logger;
        private readonly TaskCompletionSource closeTaskCompletionSource = new();
        private static readonly byte[] secureSalt = Guid.NewGuid().ToByteArray();

        public Task Closed => this.closeTaskCompletionSource.Task;

        /// <summary>
        /// 隧道标识
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        /// 传输协议
        /// </summary>
        public TransportProtocol Protocol { get; }

        public HttpTunnel(Stream inner, Guid tunnelId, TransportProtocol protocol, ILogger logger)
            : base(inner)
        {
            this.Id = tunnelId;
            this.Protocol = protocol;
            this.logger = logger;
        }

        /// <summary>
        /// 生成安全的tunnelId
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public static Guid CreateTunnelId(string clientId)
        {
            var hash32 = new XxHash32();
            Span<byte> span = stackalloc byte[16];

            // [0-3]   clientId
            Span<byte> clientIdBytes = stackalloc byte[Encoding.UTF8.GetByteCount(clientId)];
            Encoding.UTF8.GetBytes(clientId, clientIdBytes);
            hash32.Append(clientIdBytes);
            hash32.GetHashAndReset(span);

            // [4-11]  随机数 
            Random.Shared.NextBytes(span.Slice(4, 8));

            // [12-15] 校验值
            hash32.Append(span[..12]);
            hash32.Append(secureSalt);
            hash32.GetCurrentHash(span[12..]);

            return new Guid(span);
        }

        /// <summary>
        /// 校验tunnelId
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <returns></returns>
        public static bool VerifyTunnelId(Guid tunnelId)
        {
            Span<byte> span = stackalloc byte[20];
            tunnelId.TryWriteBytes(span);

            // 计算校验值，放到[16-19]
            var hash32 = new XxHash32();
            hash32.Append(span[..12]);
            hash32.Append(secureSalt);
            hash32.GetCurrentHash(span[16..]);

            var hash1 = span.Slice(12, 4);
            var hash2 = span.Slice(16, 4);
            return hash1.SequenceEqual(hash2);
        }

        public override ValueTask DisposeAsync()
        {
            this.SetClosedResult();
            return this.Inner.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            this.SetClosedResult();
            this.Inner.Dispose();
        }

        private void SetClosedResult()
        {
            if (this.closeTaskCompletionSource.TrySetResult())
            {
                Log.LogTunnelClosed(this.logger, this.Protocol, this.Id);
            }
        }


        public override string ToString()
        {
            return this.Id.ToString();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "{protocol}隧道{tunnelId}已关闭")]
            public static partial void LogTunnelClosed(ILogger logger, TransportProtocol protocol, Guid tunnelId);
        }
    }
}
