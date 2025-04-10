using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed partial class CYarpListener : ICYarpListener
    {
        private readonly CYarpConnectionFactory connectionFactory;
        private readonly CYarpConnection connection;
        private readonly ILogger logger;

        /// <summary>
        /// 获取关闭凭证
        /// </summary>
        public CancellationToken Closed => this.connection.Closed;

        public CYarpListener(
            CYarpConnectionFactory connectionFactory,
            CYarpConnection connection,
            ILogger logger)
        {
            this.connectionFactory = connectionFactory;
            this.connection = connection;
            this.logger = logger;
        }

        /// <summary>
        /// 接收CYarp服务器的传输连接
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<Stream?> AcceptAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                var tunnelId = await this.connection.ReadTunnelIdAsync(cancellationToken);
                if (tunnelId == null)
                {
                    return null;
                }

                try
                {
                    return await this.connectionFactory.CreateServerTunnelAsync(tunnelId.Value, cancellationToken);
                }
                catch (CYarpConnectException ex)
                {
                    Log.LogTunnelError(this.logger, tunnelId.Value, ex.Message);
                }
            }
        }


        public ValueTask DisposeAsync()
        {
            return this.connection.DisposeAsync();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "[{tunnelId}] 隧道遇到异常：{reason}")]
            public static partial void LogTunnelError(ILogger logger, Guid tunnelId, string? reason);
        }
    }
}
