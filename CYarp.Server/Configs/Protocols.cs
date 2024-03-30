namespace CYarp.Server.Configs
{
    /// <summary>
    /// 允许的传输协议
    /// </summary>
    public enum Protocols
    {
        /// <summary>
        /// 同时允许Http和WebSocket
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
