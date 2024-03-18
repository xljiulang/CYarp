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
        /// 转发器授权验证配置
        /// </summary>
        public AuthorizationConfig ForwardAuthorization { get; init; } = new AuthorizationConfig();

        /// <summary>
        /// 客户端TcpKeepAlive配置
        /// </summary>
        public TcpKeepAliveConfig ClientTcpKeepAlive { get; init; } = new TcpKeepAliveConfig();
    }
}
