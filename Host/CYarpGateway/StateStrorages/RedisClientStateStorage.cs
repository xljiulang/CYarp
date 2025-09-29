using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Threading.Tasks;

namespace CYarpGateway.StateStrorages
{
    /// Redis-based IClientState storage
    sealed class RedisClientStateStorage
    {
        private readonly ConnectionMultiplexer redis;
        private readonly IOptionsMonitor<GatewayOptions> gatewayOptions;

        public RedisClientStateStorage(
            IOptionsMonitor<GatewayOptions> gatewayOptions,
            IOptionsMonitor<RedisClientStateStorageOptions> redisOptions)
        {
            this.gatewayOptions = gatewayOptions;
            this.redis = ConnectionMultiplexer.Connect(redisOptions.CurrentValue.ConnectionString);
        }

        /// <summary>
        /// Find node destination value corresponding to clientId
        /// </summary>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public async ValueTask<string?> GetNodeDestinationAsync(string clientId)
        {
            RedisKey key = RedisClientStateStorageOptions.ClientNodePrefix + clientId;
            string? node = await this.redis.GetDatabase().StringGetAsync(key);
            return node != null && this.gatewayOptions.CurrentValue.Nodes.TryGetValue(node, out var destination)
                ? destination
                : null;
        }
    }
}
