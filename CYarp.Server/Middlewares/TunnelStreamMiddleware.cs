using CYarp.Server.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
    sealed class TunnelStreamMiddleware : IMiddleware
    {
        private readonly TunnelStreamFactory tunnelStreamFactory;

        public TunnelStreamMiddleware(TunnelStreamFactory tunnelStreamFactory)
        {
            this.tunnelStreamFactory = tunnelStreamFactory;
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

            if (this.tunnelStreamFactory.Contains(tunnelId) == false)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
            }
            else
            {
                var stream = await cyarpFeature.AcceptAsync();
                using var tunnelStream = new TunnelStream(stream, tunnelId);
                if (this.tunnelStreamFactory.SetResult(tunnelStream))
                {
                    await tunnelStream.Closed;
                }
            }
        }
    }
}
