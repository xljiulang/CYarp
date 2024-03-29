using Microsoft.Extensions.Logging;
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
        private readonly ClientStateChannel clientStateChannel;
        private readonly ILogger logger;

        /// <inheritdoc/>
        public int Count => this.dictionary.Count;

        public ClientManager(
            ClientStateChannel clientStateChannel,
            ILogger<ClientManager> logger)
        {
            this.clientStateChannel = clientStateChannel;
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
                await existClient.DisposeAsync();
            }

            if (this.dictionary.TryAdd(clientId, client))
            {
                Log.LogConnected(this.logger, clientId, client.Protocol, this.Count);
                await this.clientStateChannel.WriteAsync(client, connected: true, cancellationToken);
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
                    await this.clientStateChannel.WriteAsync(client, connected: false, cancellationToken);
                }
                else
                {
                    this.dictionary.TryAdd(clientId, existClient);
                }
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
            public static partial void LogConnected(ILogger logger, string clientId, TransportProtocol protocol, int count);

            [LoggerMessage(LogLevel.Warning, "[{clientId}] {protocol}长连接断开，当前客户端数为 {count}")]
            public static partial void LogDisconnected(ILogger logger, string clientId, TransportProtocol protocol, int count);
        }
    }
}
