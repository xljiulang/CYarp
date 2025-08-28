namespace CYarpGateway.StateStrorages
{
    /// <summary>
    /// redisClientState存储Options
    /// </summary>
    sealed class RedisClientStateStorageOptions
    {
        public const string ClientNodePrefix = "CYarp:Client:";

        /// <summary>
        /// redisConnection字符串
        /// </summary>
        public string ConnectionString { get; set; } = "localhost";
    }
}
