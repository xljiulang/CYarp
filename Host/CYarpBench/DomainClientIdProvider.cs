using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;

namespace CYarpBench
{
    /// <summary>
    /// 适用于和frp bench测试的IClient的Id提供者
    /// </summary>
    sealed class DomainClientIdProvider : IClientIdProvider
    {
        private const string Scheme = "CustomDomain";
        public string Name => nameof(DomainClientIdProvider);

        /// <summary>
        /// 使用Authorization的域名参数做IClient的Id
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientId"></param>
        /// <returns></returns>
        public bool TryGetClientId(HttpContext context, [MaybeNullWhen(false)] out string clientId)
        {
            if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization))
            {
                if (authorization.Scheme == Scheme)
                {
                    clientId = authorization.Parameter;
                    return string.IsNullOrEmpty(clientId) == false;
                }
            }

            clientId = null;
            return false;
        }
    }
}
