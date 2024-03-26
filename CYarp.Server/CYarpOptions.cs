using CYarp.Server.Configs;

namespace CYarp.Server
{
    /// <summary>
    /// 选项
    /// </summary>
    public class CYarpOptions
    {
        /// <summary>
        /// 获取或设置节点名称
        /// </summary>
        public string Node { get; set; } = string.Empty;

        /// <summary>
        /// 认证配置
        /// </summary>
        public AuthorizationConfig Authorization { get; set; } = new AuthorizationConfig();

        /// <summary>
        /// 连接配置
        /// </summary>
        public ConnectionConfig Connection { get; set; } = new ConnectionConfig();

        /// <summary>
        /// http隧道配置
        /// </summary>
        public HttpTunnelConfig HttpTunnel { get; set; } = new HttpTunnelConfig();
    }
}
