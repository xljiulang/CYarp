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
    sealed partial class TunnelStreamMiddleware : IMiddleware
    {
        private readonly TunnelStreamFactory tunnelStreamFactory;
        private readonly ILogger<TunnelStreamMiddleware> logger;

        public TunnelStreamMiddleware(
            TunnelStreamFactory tunnelStreamFactory,
            ILogger<TunnelStreamMiddleware> logger)
        {
            this.tunnelStreamFactory = tunnelStreamFactory;
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
                Log.LogTunnelClosed(this.logger, tunnelId);
            }
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "{tunnelId}的Tunnel已关闭")]
            public static partial void LogTunnelClosed(ILogger logger, Guid tunnelId);
        }
    }
}
