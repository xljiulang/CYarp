﻿using CYarp.Server.Clients;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
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
        private readonly TunnelStreamFactory tunnelStreamFactory;
        private readonly IClientManager clientManager;
        private readonly IOptionsMonitor<CYarpOptions> yarpOptions;
        private readonly ILogger<CYarpClientMiddleware> logger;


        public CYarpClientMiddleware(
            IHttpForwarder httpForwarder,
            TunnelStreamFactory tunnelStreamFactory,
            IClientManager clientManager,
            IOptionsMonitor<CYarpOptions> yarpOptions,
            ILogger<CYarpClientMiddleware> logger)
        {
            this.httpForwarder = httpForwarder;
            this.tunnelStreamFactory = tunnelStreamFactory;
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
            if (Uri.TryCreate(cyarpDestination.FirstOrDefault(), UriKind.Absolute, out var clientDestination) == false)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                this.LogFailureStatus(context, "请求头CYarp-Destination的值不是Uri格式");
                return;
            }

            // 身份验证
            var user = context.User;
            if (user.Identity?.IsAuthenticated == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                this.LogFailureStatus(context, "请求者的身份认证不通过");
                return;
            }

            // clientId参数验证
            var options = yarpOptions.CurrentValue;
            var clientId = user.FindFirstValue(options.ClientAuthorization.ClientIdClaimType);
            if (string.IsNullOrEmpty(clientId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                this.LogFailureStatus(context, $"用户身份不包含{options.ClientAuthorization.ClientIdClaimType}的ClaimType");
                return;
            }

            // 角色授权验证
            if (options.ClientAuthorization.AllowRoles.Length > 0 && options.ClientAuthorization.AllowRoles.Any(user.IsInRole) == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                this.LogFailureStatus(context, "请求者的角色验证不通过");
                return;
            }


            // TcpKeepAlive
            var socketFeature = context.Features.Get<IConnectionSocketFeature>();
            if (socketFeature != null)
            {
                options.ClientTcpKeepAlive.SetTcpKeepAlive(socketFeature.Socket);
            }

            var stream = await cyarpFeature.AcceptAsync();
            using var cyarpClient = new CYarpClient(stream, this.httpForwarder, options.ClientHttpHandler, tunnelStreamFactory, clientId, clientDestination);

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
