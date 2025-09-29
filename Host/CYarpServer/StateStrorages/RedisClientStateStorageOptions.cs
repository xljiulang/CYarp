namespace CYarpServer.StateStrorages
{
    /// <summary>
    /// Redis client state storage options
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
