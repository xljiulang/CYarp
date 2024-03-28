using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// IClient的授权验证
    /// </summary>
    sealed class ClientAuthorization
    {
        private readonly IAuthorizationService authorizationService;
        private readonly IAuthorizationPolicyProvider authorizationPolicyProvider;
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;

        private AuthorizationPolicy? settingPolicy;
        private string settingPolicyName = string.Empty;
        private AuthorizationType settingType = AuthorizationType.Default;

        public ClientAuthorization(
            IAuthorizationService authorizationService,
            IAuthorizationPolicyProvider authorizationPolicyProvider,
            IOptionsMonitor<CYarpOptions> cyarpOptions)
        {
            this.authorizationService = authorizationService;
            this.authorizationPolicyProvider = authorizationPolicyProvider;
            this.cyarpOptions = cyarpOptions;
        }

        public void SetAllowAnonymous()
        {
            this.settingType = AuthorizationType.Anonymous;
        }

        public void SetPolicy(AuthorizationPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(policy);

            this.settingPolicy = policy;
            this.settingType = AuthorizationType.Policy;
        }

        public void SetPolicy(string policyName)
        {
            ArgumentException.ThrowIfNullOrEmpty(policyName);

            this.settingPolicyName = policyName;
            this.settingType = AuthorizationType.PolicyName;
        }

        /// <summary>
        /// 授权验证IClient的用户信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user)
        {
            if (this.settingType == AuthorizationType.Anonymous)
            {
                return AuthorizationResult.Success();
            }

            if (this.settingType == AuthorizationType.Default)
            {
                var policy = this.cyarpOptions.CurrentValue.Authorization.GetAuthorizationPolicy();
                return await this.authorizationService.AuthorizeAsync(user, policy);
            }

            if (this.settingType == AuthorizationType.Policy)
            {
                var policy = this.settingPolicy!;
                return await this.authorizationService.AuthorizeAsync(user, policy);
            }

            var namedPolicy = await this.authorizationPolicyProvider.GetPolicyAsync(this.settingPolicyName);
            return namedPolicy == null
                ? throw new InvalidOperationException($"找不到名{this.settingPolicyName}为的PolicyName")
                : await this.authorizationService.AuthorizeAsync(user, namedPolicy);
        }

        private enum AuthorizationType
        {
            Default,
            Policy,
            PolicyName,
            Anonymous,
        }
    }
}
