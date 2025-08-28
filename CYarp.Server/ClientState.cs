namespace CYarp.Server
{
    /// <summary>
    /// ClientState
    /// </summary>
    public record ClientState
    {
        /// <summary>
        /// Client instance
        /// </summary>
        public required IClient Client { get; init; }

        /// <summary>
        /// Whether it is in connected state
        /// </summary>
        public required bool IsConnected { get; init; }
    }
}
