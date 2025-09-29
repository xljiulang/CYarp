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
        /// Allow HTTP only
        /// </summary>
        Http,

        /// <summary>
        /// Allow WebSocket only
        /// </summary>
        WebSocket,
    }
}
