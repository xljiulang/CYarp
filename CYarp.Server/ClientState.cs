namespace CYarp.Server
{
    /// <summary>
    /// Represents the state of a client
    /// </summary>
    public record ClientState
    {
        /// <summary>
        /// The client instance
        /// </summary>
        public required IClient Client { get; init; }

        /// <summary>
        /// Indicates whether the client is currently connected
        /// </summary>
        public required bool IsConnected { get; init; }
    }
}
