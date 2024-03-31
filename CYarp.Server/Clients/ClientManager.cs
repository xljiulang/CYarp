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
    sealed class ClientManager : IClientViewer
    {
        private readonly ConcurrentDictionary<string, IClient> dictionary = new();
        private readonly ClientStateChannel clientStateChannel;

        /// <inheritdoc/>
        public int Count => this.dictionary.Count;

        public ClientManager(ClientStateChannel clientStateChannel)
        {
            this.clientStateChannel = clientStateChannel;
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
        public async ValueTask<bool> RemoveAsync(IClient client, CancellationToken cancellationToken)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                if (ReferenceEquals(existClient, client))
                {
                    await this.clientStateChannel.WriteAsync(client, connected: false, cancellationToken);
                    return true;
                }
                else
                {
                    this.dictionary.TryAdd(clientId, existClient);
                }
            }
            return false;
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
    }
}
