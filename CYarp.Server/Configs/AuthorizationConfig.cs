using Microsoft.AspNetCore.Authorization;
using System;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// 客户端授权验证配置
    /// </summary>
    public class AuthorizationConfig
    {
        private AuthorizationPolicy? policy;

        /// <summary>
        /// 允许的角色
        /// 无角色表示不验证角色
        /// 角色的CiaimType为http://schemas.microsoft.com/ws/2008/06/identity/claims/role
        /// </summary>
        public string[] AllowRoles { get; set; } = Array.Empty<string>();

        /// <summary>
        /// IClient的Id对应的ClaimType
        /// 默认值为http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid
        /// 默认的IClientIdProvider在实现上依赖此属性
        /// </summary>
        public string ClientIdClaimType { get; set; } = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/sid";


        /// <summary>
        /// 获取IClient授权验证策略
        /// </summary>
        /// <returns></returns>
        public AuthorizationPolicy GetAuthorizationPolicy()
        {
            if (this.policy == null)
            {
                var builder = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireClaim(this.ClientIdClaimType);
                if (this.AllowRoles.Length > 0)
                {
                    builder.RequireRole(this.AllowRoles);
                }
                this.policy = builder.Build();
            }
            return this.policy;
        }
    }
}
