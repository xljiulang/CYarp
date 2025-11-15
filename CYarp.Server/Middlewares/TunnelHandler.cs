using CYarp.Server.Clients;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// Handles the tunnel between client and target for proxied connections
    /// </summary>
    static class TunnelHandler
    {
        /// <summary>
        /// Tunnel does not require authentication and authorization, tunnelId is secured by its validity
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
                TunnelLog.LogInvalidRequest(tunnelFactory.Logger, context.Connection.Id, "Invalid CYarp request");
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

            // Close the connection of the tunnel
            context.Abort();
            return Results.Empty;
        }
    }
}
