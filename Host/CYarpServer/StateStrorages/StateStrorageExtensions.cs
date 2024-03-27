using CYarp.Server;
using CYarpServer.StateStrorages;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    static class StateStrorageExtensions
    {
        public static ICYarpBuilder AddRedisClientStateStorage(this ICYarpBuilder builder, Action<RedisClientStateStorageOptions> configureOptions)
        {
            builder.Services.Configure(configureOptions);
            return builder.AddClientStateStorage<RedisClientStateStorage>();
        }

        public static ICYarpBuilder AddRedisClientStateStorage(this ICYarpBuilder builder, IConfiguration configureBinder)
        {
            builder.Services.Configure<RedisClientStateStorageOptions>(configureBinder);
            return builder.AddClientStateStorage<RedisClientStateStorage>();
        }
    }
}
