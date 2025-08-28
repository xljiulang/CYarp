using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// IClientIdProvider suitable for bench testing
    /// </summary>
    sealed class DomainClientIdProvider : IClientIdProvider
    {
        /// <summary>
        /// Use X-Domain header value as IClientId
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public ValueTask<string?> GetClientIdAsync(HttpContext context)
        {
            var domain = context.Request.Headers["X-Domain"].ToString();
            return ValueTask.FromResult<string?>(domain);
        }
    }
}
