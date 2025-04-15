using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于的rfp进行bench的http转发中间件
    /// </summary>
    sealed class HttpForwardHandler
    {
        /// <summary>
        /// 通过请求域名来找到对应的IClient进行转发
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
