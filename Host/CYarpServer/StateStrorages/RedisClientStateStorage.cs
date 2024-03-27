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
    /// 基于redis的IClient状态存储器
    /// </summary>
    sealed class RedisClientStateStorage : IClientStateStorage
    {
        private ConnectionMultiplexer? redis;
        private readonly IOptionsMonitor<RedisClientStateStorageOptions> redisOptions;
        private readonly ILogger<RedisClientStateStorage> logger;

        public RedisClientStateStorage(
            IOptionsMonitor<RedisClientStateStorageOptions> redisOptions,
            ILogger<RedisClientStateStorage> logger)
        {
            this.redisOptions = redisOptions;
            this.logger = logger;
        }

        public Task ResetClientStatesAsync(string node, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public async Task WriteClientStateAsync(ClientState clientState, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(clientState.Node))
            {
                this.logger.LogError("请配置CYarpOptions.Node值");
                return;
            }

            try
            {
                await this.WriteClientStateCoreAsync(clientState, cancellationToken);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "状态写入redis失败");
            }
        }

        private async Task WriteClientStateCoreAsync(ClientState clientState, CancellationToken cancellationToken)
        {
            var options = this.redisOptions.CurrentValue;
            this.redis ??= await ConnectionMultiplexer.ConnectAsync(options.ConnectionString);

            var key = $"{RedisClientStateStorageOptions.ClientNodePrefix}{clientState.Client.Id}";
            if (clientState.IsConnected)
            {
                await this.redis.GetDatabase().StringSetAsync(key, clientState.Node);
            }
            else
            {
                await this.redis.GetDatabase().KeyDeleteAsync(key);
            }
        }
    }
}
