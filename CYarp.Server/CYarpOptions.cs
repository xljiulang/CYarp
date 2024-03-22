using CYarp.Server.Configs;

namespace CYarp.Server
{
    /// <summary>
    /// 选项
    /// </summary>
    public class CYarpOptions
    {
        /// <summary>
        /// 授权验证配置
        /// </summary>
        public AuthorizationConfig Authorization { get; set; } = new AuthorizationConfig();

        /// <summary>
        /// 信号隧道配置
        /// </summary>
        public SignalTunnelConfig SignalTunnel { get; set; } = new SignalTunnelConfig();

        /// <summary>
        /// http隧道配置
        /// </summary>
        public HttpTunnelConfig HttpTunnel { get; set; } = new HttpTunnelConfig();
    }
}
