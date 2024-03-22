using CYarp.Server.Configs;

namespace CYarp.Server
{
    /// <summary>
    /// 选项
    /// </summary>
    public class CYarpOptions
    {
        /// <summary>
        /// 客户端授权验证配置
        /// </summary>
        public AuthorizationConfig ClientAuthorization { get; set; } = new AuthorizationConfig();

        /// <summary>
        /// 客户端KeepAlive配置
        /// </summary>
        public KeepAliveConfig ClientKeepAlive { get; set; } = new KeepAliveConfig();

        /// <summary>
        /// 客户端的HttpHandler配置
        /// </summary>
        public HttpHandlerConfig ClientHttpHandler { get; set; } = new HttpHandlerConfig();
    }
}
