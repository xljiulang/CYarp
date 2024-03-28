using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// 默认的IClient的Id的提供者
    /// </summary>
    sealed class DefaultClientIdProvider : IClientIdProvider
    {
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;

        public string Name => nameof(DefaultClientIdProvider);

        public DefaultClientIdProvider(IOptionsMonitor<CYarpOptions> cyarpOptions)
        {
            this.cyarpOptions = cyarpOptions;
        }

        public bool TryGetClientId(HttpContext context, [MaybeNullWhen(false)] out string clientId)
        {
            var clientIdClaimType = this.cyarpOptions.CurrentValue.Authorization.ClientIdClaimType;
            clientId = context.User.FindFirstValue(clientIdClaimType);
            return string.IsNullOrEmpty(clientId) == false;
        }
    }
}
