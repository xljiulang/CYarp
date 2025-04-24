using CYarp.Server.Clients;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// Tunnel握手处理者
    /// </summary>
    static class TunnelHanlder
    {
        /// <summary>
        /// Tunnel不需要身份验证和授权，tunnelId由其可校验性来保证安全
        /// </summary>
        /// <param name="context"></param>
        /// <param name="tunnelFactory"></param>
        /// <param name="tunnelId"></param>
        /// <returns></returns>
        public static async Task<IResult> HandleTunnelAsync(
            HttpContext context,
            TunnelFactory tunnelFactory,
            TunnelId tunnelId)
        {
            var cyarpFeature = context.Features.GetRequiredFeature<ICYarpFeature>();
            if (cyarpFeature.IsCYarpRequest == false)
            {
                TunnelLog.LogInvalidRequest(tunnelFactory.Logger, context.Connection.Id, "不是有效的CYarp请求");
                return Results.BadRequest();
            }

            if (!tunnelId.IsValid || !tunnelFactory.Contains(tunnelId))
            {
                TunnelLog.LogInvalidTunnelId(tunnelFactory.Logger, context.Connection.Id, tunnelId);
                return Results.Forbid();
            }

            var stream = await cyarpFeature.AcceptAsStreamAsync();
            var httpTunnel = new Tunnel(stream, tunnelId, cyarpFeature.Protocol);

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
    }
}
