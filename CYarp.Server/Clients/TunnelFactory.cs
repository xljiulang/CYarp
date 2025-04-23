using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// Tunnel工厂
    /// </summary> 
    sealed partial class TunnelFactory
    {
        private readonly ConcurrentDictionary<TunnelId, TaskCompletionSource<Tunnel>> tunnelCompletionSources = new();

        public ILogger<Tunnel> Logger { get; }

        public TunnelFactory(ILogger<Tunnel> logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// 创建Tunnel
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<Tunnel> CreateTunnelAsync(ClientConnection connection, CancellationToken cancellationToken)
        {
            var tunnelId = connection.NewTunnelId();
            var tunnelSource = new TaskCompletionSource<Tunnel>();
            if (this.tunnelCompletionSources.TryAdd(tunnelId, tunnelSource) == false)
            {
                throw new SystemException($"系统中已存在{tunnelId}的tunnelId");
            }

            try
            {
                Log.LogTunnelCreating(this.Logger, connection.ClientId, tunnelId);
                await connection.CreateTunnelAsync(tunnelId, cancellationToken);
                return await tunnelSource.Task.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Log.LogTunnelCreateFailure(this.Logger, connection.ClientId, tunnelId, "远程端操作超时");
                throw;
            }
            catch (Exception ex)
            {
                Log.LogTunnelCreateFailure(this.Logger, connection.ClientId, tunnelId, ex.Message);
                throw;
            }
            finally
            {
                this.tunnelCompletionSources.TryRemove(tunnelId, out _);
            }
        }

        public bool Contains(TunnelId tunnelId)
        {
            return this.tunnelCompletionSources.ContainsKey(tunnelId);
        }

        public bool SetResult(Tunnel httpTunnel)
        {
            return this.tunnelCompletionSources.TryRemove(httpTunnel.Id, out var source) && source.TrySetResult(httpTunnel);
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] 请求创建隧道{tunnelId}")]
            public static partial void LogTunnelCreating(ILogger logger, string clientId, TunnelId tunnelId);

            [LoggerMessage(LogLevel.Warning, "[{clientId}] 创建隧道{tunnelId}失败：{reason}")]
            public static partial void LogTunnelCreateFailure(ILogger logger, string clientId, TunnelId tunnelId, string? reason);
        }
    }
}
