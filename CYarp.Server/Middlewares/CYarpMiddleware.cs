using CYarp.Server.Configs;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    sealed class CYarpMiddleware : IMiddleware
    {
        private static readonly PathString cyarpPath = "/cyarp";
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;

        public CYarpMiddleware(IOptionsMonitor<CYarpOptions> cyarpOptions)
        {
            this.cyarpOptions = cyarpOptions;
        }

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var feature = context.Request.Path.StartsWithSegments(cyarpPath)
                ? new CYarpFeature(context)
                : CYarpFeature.NotCYarp;

            if (feature.IsCYarpRequest)
            {
                if (!IsAllowProtocol(this.cyarpOptions.CurrentValue.Protocols, feature.Protocol))
                {
                    context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                    return Task.CompletedTask;
                }
            }

            context.Features.Set(feature);
            return next(context);
        }

        private static bool IsAllowProtocol(Protocols protocols, TransportProtocol protocol)
        {
            if (protocols == Protocols.All)
            {
                return true;
            }

            if (protocols == Protocols.WebSocket)
            {
                return protocol == TransportProtocol.WebSocketWithHttp11 || protocol == TransportProtocol.WebSocketWithHttp2;
            }

            return protocol == TransportProtocol.Http11 || protocol == TransportProtocol.Http2;
        }
    }
}