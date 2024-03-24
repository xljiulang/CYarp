using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// 授权验证配置
    /// </summary>
    public class AuthorizationConfig
    {
        /// <summary>
        /// 允许的角色
        /// 角色的CiaimType为http://schemas.microsoft.com/ws/2008/06/identity/claims/role
        /// </summary>
        public string[] AllowRoles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// IClient的Id对应的ClaimType，默认为http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid
        /// </summary>
        public string ClientIdClaimType { get; set; } = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid";
    }
}
