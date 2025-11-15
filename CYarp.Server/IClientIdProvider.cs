using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// Provides the id for an IClient
    /// </summary>
    public interface IClientIdProvider
    {
        /// <summary>
        /// Attempts to get the id for an IClient
        /// </summary>
        /// <param name="context">The current HTTP context</param>
        /// <returns>The client id or null if not available</returns>
        ValueTask<string?> GetClientIdAsync(HttpContext context);
    }
}
