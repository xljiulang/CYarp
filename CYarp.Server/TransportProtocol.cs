namespace CYarp.Server
{
    /// <summary>
    /// Transport protocol type
    /// </summary>
    public enum TransportProtocol
    {
        /// <summary>
        /// None
        /// </summary>
        None,

        /// <summary>
        /// HTTP/1.1 Upgrade
        /// </summary>
        Http11,

        /// <summary>
        /// HTTP/2 Extended CONNECT
        /// </summary>
        Http2,

        /// <summary>
        /// WebSocket
        /// </summary>
        WebSocketWithHttp11,

        /// <summary>
        /// WebSocket over HTTP/2
        /// </summary>
        WebSocketWithHttp2
    }
}
