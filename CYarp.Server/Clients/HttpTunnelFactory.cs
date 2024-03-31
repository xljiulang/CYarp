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
        private readonly ILogger<HttpTunnel> logger;
        private readonly ConcurrentDictionary<TunnelId, TaskCompletionSource<HttpTunnel>> httpTunnelCompletionSources = new();

        public HttpTunnelFactory(ILogger<HttpTunnel> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 创建HttpTunnel
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<HttpTunnel> CreateHttpTunnelAsync(ClientConnection connection, CancellationToken cancellationToken)
        {
            var tunnelId = TunnelId.NewTunnelId(connection.ClientId);
            var httpTunnelSource = new TaskCompletionSource<HttpTunnel>();
            if (this.httpTunnelCompletionSources.TryAdd(tunnelId, httpTunnelSource) == false)
            {
                throw new SystemException($"系统中已存在{tunnelId}的tunnelId");
            }

            try
            {
                await connection.CreateHttpTunnelAsync(tunnelId, cancellationToken);
                var httpTunnel = await httpTunnelSource.Task.WaitAsync(cancellationToken);

                var httpTunnelCount = connection.IncrementHttpTunnelCount();
                httpTunnel.BindConnection(connection);

                Log.LogTunnelCreated(this.logger, connection.ClientId, httpTunnel.Protocol, tunnelId, httpTunnelCount);
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

        public bool Contains(TunnelId tunnelId)
        {
            return this.httpTunnelCompletionSources.ContainsKey(tunnelId);
        }

        public bool SetResult(HttpTunnel httpTunnel)
        {
            return this.httpTunnelCompletionSources.TryRemove(httpTunnel.Id, out var source) && source.TrySetResult(httpTunnel);
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Error, "[{clientId}] 创建隧道{tunnelId}超时")]
            public static partial void LogTunnelCreateTimeout(ILogger logger, string clientId, TunnelId tunnelId);

            [LoggerMessage(LogLevel.Information, "[{clientId}] 创建了{protocol}协议隧道{tunnelId}，当前隧道数为{tunnelCount}")]
            public static partial void LogTunnelCreated(ILogger logger, string clientId, TransportProtocol protocol, TunnelId tunnelId, int tunnelCount);
        }
    }
}
