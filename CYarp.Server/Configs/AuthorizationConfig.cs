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
        /// ClientId的ClaimType，默认为http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid
        /// </summary>
        public string ClientIdClaimType { get; init; } = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid";
    }
}
