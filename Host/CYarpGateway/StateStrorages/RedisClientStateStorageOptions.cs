namespace CYarpGateway.StateStrorages
{
    /// <summary>
    /// redis客户端状态存储选项
    /// </summary>
    sealed class RedisClientStateStorageOptions
    {
        public const string ClientNodePrefix = "CYarp:Client:";

        /// <summary>
        /// redis连接字符串
        /// </summary>
        public string ConnectionString { get; set; } = "localhost";
    }
}
