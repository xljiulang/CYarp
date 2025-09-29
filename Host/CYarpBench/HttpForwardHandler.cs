using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// HTTP forward middleware suitable for bench testing
    /// </summary>
    sealed class HttpForwardHandler
    {
        /// <summary>
        /// Find corresponding IClient by request domain and perform forwarding
        /// </summary>
        /// <param name="context"></param>
        /// <param name="clientViewer"></param>
        /// <returns></returns>
        public static async Task HandlerAsync(HttpContext context, IClientViewer clientViewer)
        {
            var domain = context.Request.Host.Host;
            if (clientViewer.TryGetValue(domain, out var client))
            {
                await client.ForwardHttpAsync(context);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }
    }
}
