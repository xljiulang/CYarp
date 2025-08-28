namespace CYarp.Server
{
    /// <summary>
    /// ClientState
    /// </summary>
    public record ClientState
    {
        /// <summary>
        /// Client实例
        /// </summary>
        public required IClient Client { get; init; }

        /// <summary>
        /// Is否AsConnectionState
        /// </summary>
        public required bool IsConnected { get; init; }
    }
}
