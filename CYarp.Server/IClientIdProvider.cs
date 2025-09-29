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
        /// Try to get client ID
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        ValueTask<string?> GetClientIdAsync(HttpContext context);
    }
}
