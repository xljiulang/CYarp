namespace CYarp.Server
{
    /// <summary>
    /// 传输协议类型
    /// </summary>
    public enum TransportProtocol
    {
        /// <summary>
        /// HTTP/1.1 Upgrade
        /// </summary>
        Http11,

        /// <summary>
        /// HTTP/2 Extended CONNECT
        /// </summary>
        HTTP2,

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
