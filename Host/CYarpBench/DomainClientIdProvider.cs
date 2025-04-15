using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于和frp bench测试的IClient的Id提供者
    /// </summary>
    sealed class DomainClientIdProvider : IClientIdProvider
    {
        private const string Scheme = "CustomDomain";

        /// <summary>
        /// 使用Authorization的域名参数做IClient的Id
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public ValueTask<string?> GetClientIdAsync(HttpContext context)
        {
            string? clientId = null;
            if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization))
            {
                if (authorization.Scheme == Scheme)
                {
                    clientId = authorization.Parameter;
                }
            }

            return ValueTask.FromResult(clientId);
        }
    }
}
