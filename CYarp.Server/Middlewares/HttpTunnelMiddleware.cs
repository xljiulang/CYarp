using CYarp.Server.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// Get /{tunnelId} HTTP/1.1
    /// Connection: Upgrade
    /// Upgrade: CYarp
    /// 
    /// :method = CONNECT
    /// :protocol = CYarp
    /// :scheme = https
    /// :path = /{tunnelId}  
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
                var stream = await cyarpFeature.AcceptAsync();
                var httpTunnel = new HttpTunnel(stream, tunnelId, context.Request.Protocol, this.logger);

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
