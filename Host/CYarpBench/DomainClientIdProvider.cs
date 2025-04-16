using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于和frp bench测试的IClient的Id提供者
    /// </summary>
    sealed class DomainClientIdProvider : IClientIdProvider
    {
        /// <summary>
        /// 使用请求头的X-Domain值做IClient的Id
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
