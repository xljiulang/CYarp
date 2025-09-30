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
        /// Accept CYarp server transport connection
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
                    var stream = await this.connectionFactory.CreateServerTunnelAsync(tunnelId.Value, cancellationToken);
                    
                    // Register the tunnel for potential cancellation via ABRT messages
                    if (stream is CYarpConnectionFactory.ServerTunnelStream serverTunnelStream)
                    {
                        this.connection.RegisterTunnel(tunnelId.Value, serverTunnelStream.CancellationTokenSource);
                        
                        // Unregister when the stream is disposed
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                // Wait for the stream to be disposed
                                while (!serverTunnelStream.CancellationToken.IsCancellationRequested)
                                {
                                    await Task.Delay(100, serverTunnelStream.CancellationToken);
                                }
                            }
                            catch
                            {
                                // Ignore cancellation exceptions
                            }
                            finally
                            {
                                this.connection.UnregisterTunnel(tunnelId.Value);
                            }
                        });
                    }
                    
                    return stream;
                }
                catch (Exception ex)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    Log.LogTunnelError(this.logger, tunnelId.Value, ex.Message);
                }
            }
        }


        /// <summary>
        /// Accept all CYarp server transport connections
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
