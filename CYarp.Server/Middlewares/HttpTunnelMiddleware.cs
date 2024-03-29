using CYarp.Server.Clients;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// HttpTunnel握手处理中间件
    /// </summary>
    sealed class HttpTunnelMiddleware : IMiddleware
    {
        private readonly HttpTunnelFactory httpTunnelFactory;
        private readonly ILogger<HttpTunnel> logger;

        public HttpTunnelMiddleware(
            HttpTunnelFactory httpTunnelFactory,
            ILogger<HttpTunnel> logger)
        {
            this.httpTunnelFactory = httpTunnelFactory;
            this.logger = logger;
        }

        /// <summary>
        /// HttpTunnel不需要身份验证和授权，而是使用Guid的随机性和tunnel创建的超时时长来保证安全
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var cyarpFeature = context.Features.GetRequiredFeature<ICYarpFeature>();
            if (cyarpFeature.IsCYarpRequest == false)
            {
                await next(context);
                return;
            }

            var target = context.Features.GetRequiredFeature<IHttpRequestFeature>().RawTarget;
            if (Guid.TryParse(target.AsSpan().TrimStart('/'), out var tunnelId) == false)
            {
                await next(context);
                return;
            }

            if (this.httpTunnelFactory.Contains(tunnelId) == false)
            {
                context.Response.StatusCode = StatusCodes.Status408RequestTimeout;
            }
            else
            {
                var stream = await cyarpFeature.AcceptAsStreamAsync();
                var httpTunnel = new HttpTunnel(stream, tunnelId, cyarpFeature.Protocol, this.logger);

                if (this.httpTunnelFactory.SetResult(httpTunnel))
                {
                    await httpTunnel.Closed;
                }
                else
                {
                    httpTunnel.Dispose();
                }
            }
        }
    }
}
