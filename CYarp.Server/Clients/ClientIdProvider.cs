using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;

namespace CYarp.Server.Clients
{
    /// <summary>
    /// IClient的Id的提供者
    /// </summary>
    sealed class ClientIdProvider : IClientIdProvider
    {
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;

        public string Name => nameof(ClientIdProvider);

        public ClientIdProvider(IOptionsMonitor<CYarpOptions> cyarpOptions)
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
