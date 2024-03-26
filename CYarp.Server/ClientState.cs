namespace CYarp.Server
{
    /// <summary>
    /// 客户端状态
    /// </summary>
    public record ClientState
    {
        /// <summary>
        /// 所在的节点名称
        /// 从CYarpOptions.Node获得
        /// </summary>
        public required string Node { get; init; }

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
