using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于rfp进行benchhttpForwardMiddleware
    /// </summary>
    sealed class HttpForwardHandler
    {
        /// <summary>
        /// 通过Request域名来找To对应IClient进行Forward
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
