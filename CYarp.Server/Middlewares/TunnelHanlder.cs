using CYarp.Server.Clients;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// Tunnel握手处理者
    /// </summary>
    static partial class TunnelHanlder
    {
        /// <summary>
        /// Tunnel不需要身份验证和授权，tunnelId本身具有随机性和服务端可校验性来保证安全
        /// </summary>
        /// <param name="tunnelFactory"></param>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="tunnelId"></param> 
        /// <returns></returns>
        public static async Task<IResult> HandleTunnelAsync(
            TunnelFactory tunnelFactory,
            ILogger<Tunnel> logger,
            HttpContext context,
            TunnelId tunnelId)
        {
            var cyarpFeature = context.Features.GetRequiredFeature<ICYarpFeature>();
            if (cyarpFeature.IsCYarpRequest == false)
            {
                Log.LogInvalidRequest(logger, context.Connection.Id, "不是有效的CYarp请求");
                return Results.BadRequest();
            }

            if (!tunnelId.IsValid || !tunnelFactory.Contains(tunnelId))
            {
                Log.LogInvalidTunnelId(logger, context.Connection.Id, tunnelId);
                return Results.Forbid();
            }

            var stream = await cyarpFeature.AcceptAsStreamAsync();
            var httpTunnel = new Tunnel(stream, tunnelId, cyarpFeature.Protocol, logger);

            if (tunnelFactory.SetResult(httpTunnel))
            {
                await httpTunnel.WaitForDisposeAsync();
            }
            else
            {
                await httpTunnel.DisposeAsync();
            }

            // 关闭通道的连接
            context.Abort();
            return Results.Empty;
        }


        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "连接{connectionId}请求无效：{message}")]
            public static partial void LogInvalidRequest(ILogger logger, string connectionId, string message);

            [LoggerMessage(LogLevel.Warning, "连接{connectionId}传递了无效的tunnelId：{tunnelId}")]
            public static partial void LogInvalidTunnelId(ILogger logger, string connectionId, TunnelId tunnelId);
        }
    }
}
