using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// HttpTunnel工厂
    /// </summary> 
    sealed partial class HttpTunnelFactory
    {
        private readonly ILogger<HttpTunnelFactory> logger;
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<HttpTunnel>> httpTunnelCompletionSources = new();

        public HttpTunnelFactory(ILogger<HttpTunnelFactory> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 创建HttpTunnel
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpTunnel> CreateAsync(ClientConnection connection, CancellationToken cancellationToken)
        {
            var tunnelId = HttpTunnel.CreateTunnelId(connection.ClientId);
            var tunnelCompletionSource = new TaskCompletionSource<HttpTunnel>();
            this.httpTunnelCompletionSources.TryAdd(tunnelId, tunnelCompletionSource);

            try
            {
                await connection.CreateHttpTunnelAsync(tunnelId, cancellationToken);
                var httpTunnel = await tunnelCompletionSource.Task.WaitAsync(cancellationToken);

                Log.LogTunnelCreated(this.logger, connection.ClientId, httpTunnel.Protocol, httpTunnel.Id);
                return httpTunnel;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested == false)
            {
                Log.LogTunnelCreateTimeout(this.logger, connection.ClientId, tunnelId);
                throw;
            }
            finally
            {
                this.httpTunnelCompletionSources.TryRemove(tunnelId, out _);
            }
        }

        public bool Contains(Guid tunnelId)
        {
            return this.httpTunnelCompletionSources.ContainsKey(tunnelId);
        }

        /// <summary>
        /// 设置TunnelStream
        /// </summary> 
        /// <param name="httpTunnel"></param>
        public bool SetResult(HttpTunnel httpTunnel)
        {
            return this.httpTunnelCompletionSources.TryRemove(httpTunnel.Id, out var tunnelAwaiter) && tunnelAwaiter.TrySetResult(httpTunnel);
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] 创建{protocol}隧道{tunnelId}成功")]
            public static partial void LogTunnelCreated(ILogger logger, string clientId, TransportProtocol protocol, Guid tunnelId);

            [LoggerMessage(LogLevel.Error, "[{clientId}] 创建Http隧道{tunnelId}超时")]
            public static partial void LogTunnelCreateTimeout(ILogger logger, string clientId, Guid tunnelId);
        }
    }
}
