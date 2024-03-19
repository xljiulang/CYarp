using CYarp.Server.Configs;

namespace CYarp.Server
{
    /// <summary>
    /// 选项
    /// </summary>
    public record CYarpOptions
    {
        /// <summary>
        /// 客户端授权验证配置
        /// </summary>
        public AuthorizationConfig ClientAuthorization { get; init; } = new AuthorizationConfig();

        /// <summary>
        /// 客户端TcpKeepAlive配置
        /// </summary>
        public TcpKeepAliveConfig ClientTcpKeepAlive { get; init; } = new TcpKeepAliveConfig();

        /// <summary>
        /// 客户端的HttpHandler配置
        /// </summary>
        public HttpHandlerConfig ClientHttpHandler { get; init; } = new HttpHandlerConfig();
    }
}
