using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于Andfrp bench测试IClientIdProvider
    /// </summary>
    sealed class DomainClientIdProvider : IClientIdProvider
    {
        /// <summary>
        /// UseRequest头X-Domain值做IClientId
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
