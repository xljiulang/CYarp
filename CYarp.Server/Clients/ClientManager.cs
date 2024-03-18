using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 默认的客户端管理器
    /// </summary>
    public partial class ClientManager : IClientManager
    {
        private readonly ILogger<ClientManager> logger;
        private readonly ConcurrentDictionary<string, IClient> dictionary = new();

        public int Count => this.dictionary.Count;

        public ClientManager(ILogger<ClientManager> logger)
        {
            this.logger = logger;
        }

        public bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IClient client)
        {
            return this.dictionary.TryGetValue(clientId, out client);
        }

        public async ValueTask<bool> AddAsync(IClient client)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                existClient.Dispose();
            }

            if (this.dictionary.TryAdd(clientId, client))
            {
                await this.HandleConnectedAsync(clientId);
                return true;
            }
            return false;
        }

        public async ValueTask RemoveAsync(IClient client)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                if (ReferenceEquals(existClient, client))
                {
                    await this.HandleDisconnectedAsync(clientId);
                }
                else
                {
                    this.dictionary.TryAdd(clientId, existClient);
                }
            }
        }

        /// <summary>
        /// 客户端连接成功
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        protected virtual ValueTask HandleConnectedAsync(string clientId)
        {
            Log.LogConnected(this.logger, clientId, this.Count);
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// 客户端断开连接
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        protected virtual ValueTask HandleDisconnectedAsync(string clientId)
        {
            Log.LogDisconnected(this.logger, clientId, this.Count);
            return ValueTask.CompletedTask;
        }

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
            [LoggerMessage(LogLevel.Information, "[{clientId}] 长连接成功，当前客户端数为 {count}")]
            public static partial void LogConnected(ILogger logger, string clientId, int count);

            [LoggerMessage(LogLevel.Warning, "[{clientId}] 长断开连接，当前客户端数为 {count}")]
            public static partial void LogDisconnected(ILogger logger, string clientId, int count);
        }
    }
}
