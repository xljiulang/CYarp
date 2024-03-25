using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 默认的客户端管理器
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    public partial class ClientManager : IClientManager
    {
        private readonly ILogger logger;
        private readonly ConcurrentDictionary<string, IClient> dictionary = new();

        /// <inheritdoc/>
        public int Count => this.dictionary.Count;

        /// <summary>
        /// 客户端管理器
        /// </summary> 
        public ClientManager()
            : this((ILogger)NullLogger<ClientManager>.Instance)
        {
        }

        /// <summary>
        /// 客户端管理器
        /// </summary>
        /// <param name="logger">日志</param>
        public ClientManager(ILogger<ClientManager> logger)
              : this((ILogger)logger)
        {
        }

        /// <summary>
        /// 客户端管理器
        /// </summary>
        /// <param name="logger">日志</param>
        protected ClientManager(ILogger logger)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IClient client)
        {
            return this.dictionary.TryGetValue(clientId, out client);
        }

        /// <inheritdoc/>
        public async ValueTask<bool> AddAsync(IClient client)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                existClient.Dispose();
            }

            if (this.dictionary.TryAdd(clientId, client))
            {
                Log.LogConnected(this.logger, clientId, client.Protocol, this.Count);
                await this.HandleConnectedAsync(client);
                return true;
            }
            return false;
        }

        /// <inheritdoc/>
        public async ValueTask RemoveAsync(IClient client)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                if (ReferenceEquals(existClient, client))
                {
                    Log.LogDisconnected(this.logger, clientId, client.Protocol, this.Count);
                    await this.HandleDisconnectedAsync(client);
                }
                else
                {
                    this.dictionary.TryAdd(clientId, existClient);
                }
            }
        }

        /// <summary>
        /// 处理客户端连接成功
        /// </summary>
        /// <param name="client">客户端</param>
        /// <returns></returns>
        protected virtual ValueTask HandleConnectedAsync(IClient client)
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 处理客户端连接断开
        /// 被挤下线的客户端实例不会触发此方法
        /// </summary>
        /// <param name="client">客户端</param>
        /// <returns></returns>
        protected virtual ValueTask HandleDisconnectedAsync(IClient client)
        {
            return ValueTask.CompletedTask;
        }

        /// <inheritdoc/>
        public IEnumerator<IClient> GetEnumerator()
        {
            foreach (var keyValue in this.dictionary)
            {
                yield return keyValue.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "[{clientId}] {protocol}长连接成功，当前客户端数为 {count}")]
            public static partial void LogConnected(ILogger logger, string clientId, string protocol, int count);

            [LoggerMessage(LogLevel.Warning, "[{clientId}] {protocol}长连接断开，当前客户端数为 {count}")]
            public static partial void LogDisconnected(ILogger logger, string clientId, string protocol, int count);
        }
    }
}
