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
        /// 允许的传输协议
        /// 默认为All
        /// </summary>
        public Protocols Protocols { get; set; } = Protocols.All;

        /// <summary>
        /// 客户端配置
        /// </summary>
        public ClientConfig Client { get; set; } = new ClientConfig();

        /// <summary>
        /// http隧道配置
        /// </summary>
        public HttpTunnelConfig HttpTunnel { get; set; } = new HttpTunnelConfig();
    }
}
