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
    sealed partial class HttpTunnelMiddleware : IMiddleware
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
        /// HttpTunnel不需要身份验证和授权，tunnelId本身具有随机性和服务端可校验性来保证安全
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
            if (TunnelId.TryParse(target.AsSpan().TrimStart('/'), out var tunnelId) == false)
            {
                await next(context);
                return;
            }

            if (tunnelId.IsValid && this.httpTunnelFactory.Contains(tunnelId))
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
            else
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                Log.LogInvalidTunnelId(this.logger, context.Connection.Id, tunnelId);
            }
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "连接{connectionId}传递了无效的tunnelId：{tunnelId}")]
            public static partial void LogInvalidTunnelId(ILogger logger, string connectionId, TunnelId tunnelId);
        }
    }
}
