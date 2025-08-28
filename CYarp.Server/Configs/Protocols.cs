namespace CYarp.Server.Configs
{
    /// <summary>
    /// 允许TransportProtocol
    /// </summary>
    public enum Protocols
    {
        /// <summary>
        /// 同时允许HttpAndWebSocket
        /// </summary>
        All,

        /// <summary>
        /// 只允许Http
        /// </summary>
        Http,

        /// <summary>
        /// 只允许WebSocket
        /// </summary>
        WebSocket,
    }
}
