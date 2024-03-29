using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Timeouts;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// Get {PATH} HTTP/1.1
    /// Connection: Upgrade
    /// Upgrade: CYarp  
    /// 
    /// :method = CONNECT
    /// :protocol = CYarp
    /// :scheme = https
    /// </summary>
    sealed class CYarpMiddleware : IMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var isHttp2 = context.Request.Protocol == HttpProtocol.Http2;
            var webSocketManager = context.WebSockets;
            var upgradeHeader = context.Request.Headers.Upgrade;
            var upgradeFeature = context.Features.GetRequiredFeature<IHttpUpgradeFeature>();
            var connectFeature = context.Features.Get<IHttpExtendedConnectFeature>();
            var requestTimeoutFeature = context.Features.Get<IHttpRequestTimeoutFeature>();

            var feature = new CYarpFeature(isHttp2, webSocketManager, upgradeHeader, upgradeFeature, connectFeature, requestTimeoutFeature);
            context.Features.Set<ICYarpFeature>(feature);
            return next(context);
        }
    }
}