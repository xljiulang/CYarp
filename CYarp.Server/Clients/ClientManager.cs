using System;
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
    sealed class ClientManager : IClientViewer, IDisposable
    {
        private readonly SemaphoreSlim semaphore = new(1, 1);
        private readonly ClientStateChannel clientStateChannel;
        private readonly ConcurrentDictionary<string, IClient> dictionary = new();


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
        /// <returns></returns>
        public async ValueTask<bool> AddAsync(IClient client)
        {
            try
            {
                await this.semaphore.WaitAsync();
                return await this.AddCoreAsync(client);
            }
            finally
            {
                this.semaphore.Release();
            }
        }

        private async ValueTask<bool> AddCoreAsync(IClient client)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                await existClient.DisposeAsync();
            }

            if (this.dictionary.TryAdd(clientId, client))
            {
                await this.clientStateChannel.WriteAsync(client, connected: true, default);
                return true;
            }
            return false;
        }


        /// <summary>
        /// 移除客户端实例
        /// </summary>
        /// <param name="client">客户端实例</param> 
        /// <returns></returns>
        public async ValueTask<bool> RemoveAsync(IClient client)
        {
            try
            {
                await this.semaphore.WaitAsync();
                return await this.RemoveCoreAsync(client);
            }
            finally
            {
                this.semaphore.Release();
            }
        }


        private async ValueTask<bool> RemoveCoreAsync(IClient client)
        {
            var clientId = client.Id;
            if (this.dictionary.TryRemove(clientId, out var existClient))
            {
                if (ReferenceEquals(existClient, client))
                {
                    await this.clientStateChannel.WriteAsync(client, connected: false, default);
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


        public void Dispose()
        {
            this.semaphore.Dispose();
        }
    }
}
