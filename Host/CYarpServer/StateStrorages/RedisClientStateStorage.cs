using CYarp.Server;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CYarpServer.StateStrorages
{
    /// <summary>
    /// 基于redisIClientState存储器
    /// </summary>
    sealed class RedisClientStateStorage : IClientStateStorage
    {
        private ConnectionMultiplexer? redis;
        private readonly string node;
        private readonly IOptionsMonitor<RedisClientStateStorageOptions> redisOptions;
        private readonly ILogger<RedisClientStateStorage> logger;

        public RedisClientStateStorage(
            IOptions<CYarpOptions> cyarpOptions,
            IOptionsMonitor<RedisClientStateStorageOptions> redisOptions,
            ILogger<RedisClientStateStorage> logger)
        {
            ArgumentException.ThrowIfNullOrEmpty(cyarpOptions.Value.Node);

            this.node = cyarpOptions.Value.Node;
            this.redisOptions = redisOptions;
            this.logger = logger;
        }

        public Task InitClientStatesAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task WriteClientStateAsync(ClientState clientState, CancellationToken cancellationToken)
        {
            try
            {
                await this.WriteClientStateCoreAsync(clientState, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "State写入redisFailure");
            }
        }

        private async Task WriteClientStateCoreAsync(ClientState clientState, CancellationToken cancellationToken)
        {
            var options = this.redisOptions.CurrentValue;
            this.redis ??= await ConnectionMultiplexer.ConnectAsync(options.ConnectionString);

            var key = RedisClientStateStorageOptions.ClientNodePrefix + clientState.Client.Id;
            if (clientState.IsConnected)
            {
                await this.redis.GetDatabase().StringSetAsync(key, this.node);
            }
            else
            {
                await this.redis.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }
}
