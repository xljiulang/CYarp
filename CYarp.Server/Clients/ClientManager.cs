using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 客户端管理器
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    sealed partial class ClientManager : IClientViewer
    {
        private readonly ConcurrentDictionary<string, IClient> dictionary = new();
        private readonly IEnumerable<IClientStateStorage> stateStorages;
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;
        private readonly ILogger logger;

        /// <inheritdoc/>
        public int Count => this.dictionary.Count;

        public ClientManager(
            IEnumerable<IClientStateStorage> stateStorages,
            IOptionsMonitor<CYarpOptions> cyarpOptions,
            ILogger<ClientManager> logger)
        {
            this.stateStorages = stateStorages;
            this.cyarpOptions = cyarpOptions;
            this.logger = logger;
        }

        /// <inheritdoc/>
        public bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IClient client)
        {
            return this.dictionary.TryGetValue(clientId, out client);
        }

        /// <summary>
        /// 添加客户端实例
        /// </summary>
        /// <param name="client">客户端实例</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask<bool> AddAsync(IClient client, CancellationToken cancellationToken)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                existClient.Dispose();
            }

            if (this.dictionary.TryAdd(clientId, client))
            {
                Log.LogConnected(this.logger, clientId, client.Protocol, this.Count);
                await this.HandleClientStateAsync(client, connected: true, cancellationToken);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 移除客户端实例
        /// </summary>
        /// <param name="client">客户端实例</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async ValueTask RemoveAsync(IClient client, CancellationToken cancellationToken)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                if (ReferenceEquals(existClient, client))
                {
                    Log.LogDisconnected(this.logger, clientId, client.Protocol, this.Count);
                    await this.HandleClientStateAsync(client, connected: false, cancellationToken);
                }
                else
                {
                    this.dictionary.TryAdd(clientId, existClient);
                }
            }
        }

        private async ValueTask HandleClientStateAsync(IClient client, bool connected, CancellationToken cancellationToken)
        {
            var clientState = new ClientState
            {
                Node = this.cyarpOptions.CurrentValue.Node,
                Client = client,
                IsConnected = connected
            };

            foreach (var storage in this.stateStorages)
            {
                await storage.WriteClientStateAsync(clientState, cancellationToken);
            }
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
