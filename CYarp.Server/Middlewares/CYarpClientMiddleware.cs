using CYarp.Server.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// IClient的授权验证、实例创建和生命周期管理中间件
    /// </summary>
    sealed partial class CYarpClientMiddleware : IMiddleware
    {
        private readonly ClientAuthorization clientAuthorization;
        private readonly IClientIdProvider clientIdProvider;
        private readonly IHttpForwarder httpForwarder;
        private readonly HttpTunnelFactory httpTunnelFactory;
        private readonly ClientManager clientManager;
        private readonly IOptionsMonitor<CYarpOptions> yarpOptions;
        private readonly ILogger<CYarpClient> logger;

        private const string CYarpTargetUriHeader = "CYarp-TargetUri";

        public CYarpClientMiddleware(
            ClientAuthorization clientAuthorization,
            IClientIdProvider clientIdProvider,
            IHttpForwarder httpForwarder,
            HttpTunnelFactory httpTunnelFactory,
            ClientManager clientManager,
            IOptionsMonitor<CYarpOptions> yarpOptions,
            ILogger<CYarpClient> logger)
        {
            this.clientAuthorization = clientAuthorization;
            this.clientIdProvider = clientIdProvider;
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
                context.Request.Headers.TryGetValue(CYarpTargetUriHeader, out var targetUri) == false)
            {
                await next(context);
                return;
            }

            // CYarp-TargetUri头格式验证
            if (Uri.TryCreate(targetUri, UriKind.Absolute, out var clientTargetUri) == false)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                this.LogFailureStatus(context, $"请求头{CYarpTargetUriHeader}的值不是Uri格式");
                return;
            }

            // 身份验证
            var clinetUser = context.User;
            var authorizationResult = await this.clientAuthorization.AuthorizeAsync(clinetUser);
            if (authorizationResult.Succeeded == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var failedItem = authorizationResult.Failure.FailedRequirements.FirstOrDefault();
                var message = failedItem?.ToString() ?? "请求者的身份认证不通过";
                this.LogFailureStatus(context, message);
                return;
            }

            // 查找clientId
            if (this.clientIdProvider.TryGetClientId(context, out var clientId) == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var message = $"{this.clientIdProvider.Name}无法获取到IClient的Id";
                this.LogFailureStatus(context, message);
                return;
            }

            var options = yarpOptions.CurrentValue;
            var stream = await cyarpFeature.AcceptAsync();
            var connection = new CYarpConnection(stream);
            using var cyarpClient = new CYarpClient(connection, options.Connection, this.httpForwarder, options.HttpTunnel, httpTunnelFactory, clientId, clientTargetUri, context, this.logger);

            if (await this.clientManager.AddAsync(cyarpClient, default))
            {
                await cyarpClient.WaitForCloseAsync();
                await this.clientManager.RemoveAsync(cyarpClient, default);
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
