using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
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

        private readonly List<string> policyNameList = new();
        private readonly List<AuthorizationPolicy> policyList = new();
        private AuthorizationType authorizationType = AuthorizationType.Default;

        private AuthorizationPolicy? cachePolicy;

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
            this.authorizationType = AuthorizationType.Anonymous;
        }

        public void AddPolicy(Action<AuthorizationPolicyBuilder> configurePolicy)
        {
            ArgumentNullException.ThrowIfNull(configurePolicy);

            var builder = new AuthorizationPolicyBuilder();
            configurePolicy(builder);
            this.AddPolicy(builder.Build());
        }

        public void AddPolicy(AuthorizationPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(policy);

            this.policyList.Add(policy);
            this.authorizationType = AuthorizationType.Policy;
        }

        public void AddPolicy(string policyName)
        {
            ArgumentException.ThrowIfNullOrEmpty(policyName);

            this.policyNameList.Add(policyName);
            this.authorizationType = AuthorizationType.Policy;
        }

        /// <summary>
        /// 授权验证IClient的用户信息
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<AuthorizationResult> AuthorizeAsync(ClaimsPrincipal user)
        {
            if (this.authorizationType == AuthorizationType.Anonymous)
            {
                return AuthorizationResult.Success();
            }

            if (this.authorizationType == AuthorizationType.Default)
            {
                var defaultPolicy = this.cyarpOptions.CurrentValue.Authorization.GetAuthorizationPolicy();
                return await this.authorizationService.AuthorizeAsync(user, defaultPolicy);
            }

            this.cachePolicy ??= await this.CreatePolicyAsync();
            return await this.authorizationService.AuthorizeAsync(user, this.cachePolicy);
        }


        private async Task<AuthorizationPolicy> CreatePolicyAsync()
        {
            var builder = new AuthorizationPolicyBuilder();
            foreach (var policy in this.policyList)
            {
                builder.Combine(policy);
            }

            foreach (var policyName in this.policyNameList)
            {
                var policy = await this.authorizationPolicyProvider.GetPolicyAsync(policyName);
                builder.Combine(policy ?? throw new InvalidOperationException($"找不到名{policyName}为的PolicyName"));
            }
            return builder.Build();
        }

        private enum AuthorizationType
        {
            Default,
            Policy,
            Anonymous,
        }
    }
}
