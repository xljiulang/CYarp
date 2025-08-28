using CYarp.Server.Clients;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// Tunnel handshake handler
    /// </summary>
    static class TunnelHanlder
    {
        /// <summary>
        /// Tunnel does not require identity verification and authorization, tunnelId ensures security through its verifiability
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
                TunnelLog.LogInvalidRequest(tunnelFactory.Logger, context.Connection.Id, "Not a valid CYarp request");
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

            // Close tunnel connection
            context.Abort();
            return Results.Empty;
        }
    }
}
