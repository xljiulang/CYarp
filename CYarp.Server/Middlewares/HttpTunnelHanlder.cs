using CYarp.Server.Clients;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// HttpTunnel握手处理中间件
    /// </summary>
    sealed partial class HttpTunnelHanlder
    {
        private readonly HttpTunnelFactory httpTunnelFactory;
        private readonly ILogger<HttpTunnel> logger;

        public HttpTunnelHanlder(
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
        /// <param name="tunnelId"></param> 
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, TunnelId tunnelId)
        {
            var cyarpFeature = context.Features.GetRequiredFeature<ICYarpFeature>();
            if (cyarpFeature.IsCYarpRequest == false)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                Log.LogFailureStatus(this.logger, context.Connection.Id, context.Response.StatusCode, "不是有效的CYarp请求");
                return;
            }

            if (!tunnelId.IsValid || !this.httpTunnelFactory.Contains(tunnelId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                Log.LogInvalidTunnelId(this.logger, context.Connection.Id, tunnelId);
                return;
            }

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

            // 关闭通道的连接
            context.Abort();
        }


        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "连接{connectionId}触发{statusCode}状态码: {message}")]
            public static partial void LogFailureStatus(ILogger logger, string connectionId, int statusCode, string message);

            [LoggerMessage(LogLevel.Warning, "连接{connectionId}传递了无效的tunnelId：{tunnelId}")]
            public static partial void LogInvalidTunnelId(ILogger logger, string connectionId, TunnelId tunnelId);
        }
    }
}
