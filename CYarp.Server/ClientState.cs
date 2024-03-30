namespace CYarp.Server
{
    /// <summary>
    /// 客户端状态
    /// </summary>
    public record ClientState
    {
        /// <summary>
        /// 客户端实例
        /// </summary>
        public required IClient Client { get; init; }

        /// <summary>
        /// 是否为连接状态
        /// </summary>
        public required bool IsConnected { get; init; }
    }
}
