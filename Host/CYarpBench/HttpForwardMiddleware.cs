using CYarp.Server;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于的rfp进行bench的http转发中间件
    /// </summary>
    sealed class HttpForwardMiddleware : IMiddleware
    {
        private readonly IClientViewer clientViewer;

        public HttpForwardMiddleware(IClientViewer clientViewer)
        {
            this.clientViewer = clientViewer;
        }

        /// <summary>
        /// 通过请求域名来找到对应的IClient进行转发
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var domain = context.Request.Host.Host;
            if (this.clientViewer.TryGetValue(domain, out var client))
            {
                await client.ForwardHttpAsync(context);
            }
            else
            {
                await next(context);
            }
        }
    }
}
