using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// TunnelStream工厂
    /// </summary> 
    sealed partial class TunnelStreamFactory
    {
        private readonly ILogger<TunnelStreamFactory> logger;
        private readonly TimeSpan timeout = TimeSpan.FromSeconds(10d);
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<TunnelStream>> tunnelStreamCompletionSources = new();

        public TunnelStreamFactory(ILogger<TunnelStreamFactory> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 创建TunnelStream
        /// </summary>
        /// <param name="client"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask<TunnelStream> CreateAsync(IClient client, CancellationToken cancellationToken)
        {
            var tunnelId = Guid.NewGuid();
            var tunnelCompletionSource = new TaskCompletionSource<TunnelStream>();
            var registration = cancellationToken.Register(() => tunnelCompletionSource.TrySetException(new OperationCanceledException()));
            this.tunnelStreamCompletionSources.TryAdd(tunnelId, tunnelCompletionSource);

            try
            {
                await client.CreateTunnelAsync(tunnelId, cancellationToken);
                var tunnelStream = await tunnelCompletionSource.Task.WaitAsync(this.timeout, cancellationToken);

                Log.LogTunnelCreated(this.logger, client.Id, tunnelId);
                return tunnelStream;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested == false)
            {
                Log.LogTunnelCreateTimeout(this.logger, client.Id, tunnelId);
                throw;
            }
            finally
            {
                registration.Dispose();
                this.tunnelStreamCompletionSources.TryRemove(tunnelId, out _);
            }
        }

        public bool Contains(Guid tunnelId)
        {
            return this.tunnelStreamCompletionSources.ContainsKey(tunnelId);
        }

        /// <summary>
        /// 设置TunnelStream
        /// </summary> 
        /// <param name="tunnelStream"></param>
        public bool SetResult(TunnelStream tunnelStream)
        {
            return this.tunnelStreamCompletionSources.TryRemove(tunnelStream.Id, out var tunnelAwaiter) && tunnelAwaiter.TrySetResult(tunnelStream);
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] 创建了{tunnelId}的Tunnel")]
            public static partial void LogTunnelCreated(ILogger logger, string clientId, Guid tunnelId);

            [LoggerMessage(LogLevel.Error, "[{clientId}] 创建{tunnelId}的Tunnel超时")]
            public static partial void LogTunnelCreateTimeout(ILogger logger, string clientId, Guid tunnelId);
        }
    }
}
