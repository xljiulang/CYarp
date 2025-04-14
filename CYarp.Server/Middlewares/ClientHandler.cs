using CYarp.Server.Clients;
using CYarp.Server.Features;
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
    sealed partial class ClientHandler
    {
        private readonly IClientIdProvider clientIdProvider;
        private readonly IHttpForwarder httpForwarder;
        private readonly HttpTunnelFactory httpTunnelFactory;
        private readonly ClientManager clientManager;
        private readonly IOptionsMonitor<CYarpOptions> yarpOptions;
        private readonly ILogger<Client> logger;

        private const string CYarpTargetUriHeader = "CYarp-TargetUri";

        public ClientHandler(
            IClientIdProvider clientIdProvider,
            IHttpForwarder httpForwarder,
            HttpTunnelFactory httpTunnelFactory,
            ClientManager clientManager,
            IOptionsMonitor<CYarpOptions> yarpOptions,
            ILogger<Client> logger)
        {
            this.clientIdProvider = clientIdProvider;
            this.httpForwarder = httpForwarder;
            this.httpTunnelFactory = httpTunnelFactory;
            this.clientManager = clientManager;
            this.yarpOptions = yarpOptions;
            this.logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var cyarpFeature = context.Features.GetRequiredFeature<ICYarpFeature>();
            if (cyarpFeature.IsCYarpRequest == false || context.Request.Headers.TryGetValue(CYarpTargetUriHeader, out var targetUri) == false)
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

            // 授权验证
            var authorizationResult = await this.clientPolicyService.AuthorizeAsync(context);
            if (authorizationResult.Succeeded == false)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                var failedItem = authorizationResult.AuthorizationFailure?.FailedRequirements.FirstOrDefault();
                var message = failedItem?.ToString() ?? "请求者的身份验证不通过";
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
            var stream = await cyarpFeature.AcceptAsSafeWriteStreamAsync();
            var connection = new ClientConnection(clientId, stream, options.Connection, this.logger);

            var disconnected = false;
            await using (var client = new Client(connection, this.httpForwarder, options.HttpTunnel, httpTunnelFactory, clientTargetUri, context))
            {
                if (await this.clientManager.AddAsync(client, default))
                {
                    Log.LogConnected(this.logger, clientId, cyarpFeature.Protocol, this.clientManager.Count);
                    await connection.WaitForCloseAsync();
                    disconnected = await this.clientManager.RemoveAsync(client, default);
                }
            }

            if (disconnected)
            {
                Log.LogDisconnected(this.logger, clientId, cyarpFeature.Protocol, this.clientManager.Count);
            }

            // 关闭连接
            context.Abort();
        }

        private void LogFailureStatus(HttpContext context, string message)
        {
            Log.LogFailureStatus(this.logger, context.Connection.Id, context.Response.StatusCode, message);
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "连接{connectionId}触发{statusCode}状态码: {message}")]
            public static partial void LogFailureStatus(ILogger logger, string connectionId, int statusCode, string message);

            [LoggerMessage(LogLevel.Information, "[{clientId}] {protocol}长连接成功，系统当前客户端总数为{count}")]
            public static partial void LogConnected(ILogger logger, string clientId, TransportProtocol protocol, int count);

            [LoggerMessage(LogLevel.Warning, "[{clientId}] {protocol}长连接断开，系统当前客户端总数为{count}")]
            public static partial void LogDisconnected(ILogger logger, string clientId, TransportProtocol protocol, int count);
        }
    }
}
