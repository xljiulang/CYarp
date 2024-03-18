using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    sealed partial class HttpForwardMiddleware : IMiddleware
    {
        private readonly IClientManager clientManager;
        private readonly IOptionsMonitor<CYarpOptions> cyarpOptions;
        private readonly ILogger<HttpForwardMiddleware> logger;

        public HttpForwardMiddleware(
            IClientManager clientManager,
            IOptionsMonitor<CYarpOptions> cyarpOptions,
            ILogger<HttpForwardMiddleware> logger)
        {
            this.clientManager = clientManager;
            this.cyarpOptions = cyarpOptions;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var user = context.User;
            if (user.Identity?.IsAuthenticated == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var options = this.cyarpOptions.CurrentValue.ForwardAuthorization;
            var clientId = user.FindFirstValue(options.ClientIdClaimType);
            if (clientId == null)
            {
                Log.ClientIdClaimTypeIsRequired(this.logger, options.ClientIdClaimType);
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            // 角色授权验证
            if (options.AllowRoles.Length > 0 && options.AllowRoles.Any(user.IsInRole) == false)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return;
            }

            if (this.clientManager.TryGetValue(clientId, out var client) == false)
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
            else
            {
                var stopwatch = Stopwatch.StartNew();
                await client.ForwardHttpAsync(context);
                stopwatch.Stop();

                var destination = new Uri(client.Destination, context.Request.Path);
                Log.ForwardHttpRequest(this.logger, clientId, context.Request.Method, destination, context.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
            }
        }


        static partial class Log
        {

            [LoggerMessage(LogLevel.Warning, "用户身份不包含ClaimType: {clientIdClaimType}")]
            public static partial void ClientIdClaimTypeIsRequired(ILogger logger, string clientIdClaimType);

            [LoggerMessage(LogLevel.Information, "[{clientId}] {method} {destination} response {statusCode} elapsed {elapsed} ms")]
            public static partial void ForwardHttpRequest(ILogger logger, string clientId, string method, Uri destination, int statusCode, double elapsed);
        }
    }
}