using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// IClient的授权策略服务
    /// </summary>
    sealed class ClientPolicyService
    {
        private readonly IAuthorizationPolicyProvider authorizationPolicyProvider;
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;

        private readonly List<string> policyNameList = new();
        private readonly List<AuthorizationPolicy> policyList = new();
        private AuthorizationType authorizationType = AuthorizationType.Default;

        private AuthorizationPolicy? cachePolicy;

        public ClientPolicyService(
            IAuthorizationPolicyProvider authorizationPolicyProvider,
            IOptionsMonitor<CYarpOptions> cyarpOptions)
        {
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
        /// 使用策略授权验证IClient
        /// </summary>
        /// <param name="context"></param> 
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task<PolicyAuthorizationResult> AuthorizeAsync(HttpContext context)
        {
            if (this.authorizationType == AuthorizationType.Anonymous)
            {
                return PolicyAuthorizationResult.Success();
            }

            var policy = await this.GetPolicyAsync();
            var policyEvaluator = context.RequestServices.GetRequiredService<IPolicyEvaluator>();

            var authenticateResult = await policyEvaluator.AuthenticateAsync(policy, context);
            if (authenticateResult.Succeeded)
            {
                var authenticateResultFeature = context.Features.Get<IAuthenticateResultFeature>();
                if (authenticateResultFeature != null)
                {
                    authenticateResultFeature.AuthenticateResult = authenticateResult;
                }
                else
                {
                    var instance = new AuthenticationFeatures(authenticateResult);
                    context.Features.Set<IHttpAuthenticationFeature>(instance);
                    context.Features.Set<IAuthenticateResultFeature>(instance);
                }
            }

            return await policyEvaluator.AuthorizeAsync(policy, authenticateResult, context, context);
        }


        private async Task<AuthorizationPolicy> GetPolicyAsync()
        {
            if (this.authorizationType == AuthorizationType.Default)
            {
                return this.cyarpOptions.CurrentValue.Authorization.GetAuthorizationPolicy();
            }

            this.cachePolicy ??= await this.CombinePolicyAsync();
            return this.cachePolicy;
        }


        private async Task<AuthorizationPolicy> CombinePolicyAsync()
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

        private class AuthenticationFeatures : IHttpAuthenticationFeature, IAuthenticateResultFeature
        {
            private ClaimsPrincipal? user;

            private AuthenticateResult? result;

            public AuthenticateResult? AuthenticateResult
            {
                get
                {
                    return result;
                }
                set
                {
                    result = value;
                    user = result?.Principal;
                }
            }

            public ClaimsPrincipal? User
            {
                get
                {
                    return user;
                }
                set
                {
                    user = value;
                    result = null;
                }
            }

            public AuthenticationFeatures(AuthenticateResult result)
            {
                this.AuthenticateResult = result;
            }
        }
    }
}
