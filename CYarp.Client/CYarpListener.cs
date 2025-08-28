using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed partial class CYarpListener : ICYarpListener
    {
        private readonly CYarpConnectionFactory connectionFactory;
        private readonly CYarpConnection connection;
        private readonly ILogger logger;

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
        /// 接收CYarpServerTransportConnection
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
                catch (Exception ex)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.LogTunnelError(this.logger, tunnelId.Value, ex.Message);
                }
            }
        }


        /// <summary>
        /// 接收CYarpServer所有TransportConnection
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async IAsyncEnumerable<Stream> AcceptAllAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var stream = await this.AcceptAsync(cancellationToken);
                if (stream == null)
                {
                    break;
                }
                yield return stream;
            }
        }


        public ValueTask DisposeAsync()
        {
            return this.connection.DisposeAsync();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "[{tunnelId}] TunnelEncounteredException：{reason}")]
            public static partial void LogTunnelError(ILogger logger, Guid tunnelId, string? reason);
        }
    }
}
