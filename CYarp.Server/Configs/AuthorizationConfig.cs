using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// 授权验证配置
    /// </summary>
    public record AuthorizationConfig
    {
        /// <summary>
        /// 允许的角色
        /// </summary>
        public string[] AllowRoles { get; init; } = Array.Empty<string>();

        /// <summary>
        /// ClientId的ClaimType，默认为ClientId
        /// </summary>
        public string ClientIdClaimType { get; init; } = "ClientId";
    }
}
