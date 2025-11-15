namespace CYarp.Server.Configs
{
    /// <summary>
    /// Allowed transport protocols
    /// </summary>
    public enum Protocols
    {
        /// <summary>
        /// Allow both HTTP and WebSocket
        /// </summary>
        All,

        /// <summary>
        /// Allow only HTTP
        /// </summary>
        Http,

        /// <summary>
        /// Allow only WebSocket
        /// </summary>
        WebSocket,
    }
}
