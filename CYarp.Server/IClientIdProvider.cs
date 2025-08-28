using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// IClientIdProvider
    /// </summary>
    public interface IClientIdProvider
    {
        /// <summary>
        /// 尝试GetIClientId
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        ValueTask<string?> GetClientIdAsync(HttpContext context);
    }
}
