using CYarp.Server.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// Get / HTTP/1.1
    /// Connection: Upgrade
    /// Upgrade: CYarp
    /// CYarp-Destination = {URI}
    /// 
    /// :method = CONNECT
    /// :protocol = CYarp
    /// :scheme = https
    /// :path = /  
    /// CYarp-Destination = {URI}
    /// </summary>
    sealed partial class CYarpClientMiddleware : IMiddleware
    {
        private static readonly string cyarpDestinationHeader = "CYarp-Destination";
        private readonly IHttpForwarder httpForwarder;
        private readonly HttpTunnelFactory httpTunnelFactory;
        private readonly IClientManager clientManager;
        private readonly IOptionsMonitor<CYarpOptions> yarpOptions;
        private readonly ILogger<CYarpClient> logger;


        public CYarpClientMiddleware(
            IHttpForwarder httpForwarder,
            HttpTunnelFactory httpTunnelFactory,
            IClientManager clientManager,
            IOptionsMonitor<CYarpOptions> yarpOptions,
            ILogger<CYarpClient> logger)
        {
            this.httpForwarder = httpForwarder;
            this.httpTunnelFactory = httpTunnelFactory;
            this.clientManager = clientManager;
            this.yarpOptions = yarpOptions;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var cyarpFeature = context.Features.GetRequiredFeature<ICYarpFeature>();
            if (cyarpFeature.IsCYarpRequest == false ||
                context.Request.Headers.TryGetValue(cyarpDestinationHeader, out var cyarpDestination) == false)
            {
                await next(context);
                return;
            }

            // CYarp-Destination头格式验证
            if (Uri.TryCreate(cyarpDestination, UriKind.Absolute, out var clientDestination) == false)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                this.LogFailureStatus(context, "请求头CYarp-Destination的值不是Uri格式");
                return;
            }

            // 身份验证
            var clinetUser = context.User;
            if (clinetUser.Identity?.IsAuthenticated == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                this.LogFailureStatus(context, "请求者的身份认证不通过");
                return;
            }

            // clientId参数验证
            var options = yarpOptions.CurrentValue;
            var clientId = clinetUser.FindFirstValue(options.Authorization.ClientIdClaimType);
            if (string.IsNullOrEmpty(clientId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                this.LogFailureStatus(context, $"用户身份不包含{options.Authorization.ClientIdClaimType}的ClaimType");
                return;
            }

            // 角色授权验证
            if (options.Authorization.AllowRoles.Length > 0 && options.Authorization.AllowRoles.Any(clinetUser.IsInRole) == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                this.LogFailureStatus(context, "请求者的角色验证不通过");
                return;
            }

            var stream = await cyarpFeature.AcceptAsync();
            var connection = new CYarpConnection(stream);
            var clientProtocol = context.Request.Protocol;
            var remoteAddress = context.Connection.RemoteIpAddress;
            var remoteEndPoint = remoteAddress == null ? null : new IPEndPoint(remoteAddress, context.Connection.RemotePort);
            using var cyarpClient = new CYarpClient(connection, options.Connection, this.httpForwarder, options.HttpTunnel, httpTunnelFactory, clientId, clientDestination, clinetUser, clientProtocol, remoteEndPoint, this.logger);

            if (await this.clientManager.AddAsync(cyarpClient))
            {
                await cyarpClient.WaitForCloseAsync();
                await this.clientManager.RemoveAsync(cyarpClient);
            }
        }

        private void LogFailureStatus(HttpContext context, string message)
        {
            Log.LogFailureStatus(this.logger, context.Connection.Id, context.Response.StatusCode, message);
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "连接{connectionId}触发{statusCode}状态码: {message}")]
            public static partial void LogFailureStatus(ILogger logger, string connectionId, int statusCode, string message);
        }
    }
}
