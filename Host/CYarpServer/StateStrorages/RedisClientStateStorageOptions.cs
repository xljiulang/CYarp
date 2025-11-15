namespace CYarpServer.StateStrorages
{
    /// <summary>
    /// Options for Redis-based client state storage
    /// </summary>
    sealed class RedisClientStateStorageOptions
    {
        public const string ClientNodePrefix = "CYarp:Client:";

        /// <summary>
        /// Redis connection string
        /// </summary>
        public string ConnectionString { get; set; } = "localhost";
    }
}
