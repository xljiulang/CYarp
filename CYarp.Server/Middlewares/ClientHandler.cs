using CYarp.Server.Clients;
using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server.Middlewares
{
    /// <summary>
    /// IClient的授权验证、实例创建和生命周期管理中间件
    /// </summary>
    sealed partial class ClientHandler
    {
        private const string CYarpTargetUriHeader = "CYarp-TargetUri";
        private readonly Func<HttpContext, ValueTask<string?>> clientIdProvider;

        public ClientHandler(Func<HttpContext, ValueTask<string?>> clientIdProvider)
        {
            this.clientIdProvider = clientIdProvider;
        }

        public async Task<IResult> HandleClientAsync(
            HttpContext context,
            IHttpForwarder httpForwarder,
            HttpTunnelFactory httpTunnelFactory,
            ClientManager clientManager,
            IOptionsMonitor<CYarpOptions> yarpOptions,
            ILogger<Client> logger)
        {
            var cyarpFeature = context.Features.GetRequiredFeature<ICYarpFeature>();
            if (cyarpFeature.IsCYarpRequest == false)
            {
                Log.LogInvalidRequest(logger, context.Connection.Id, "不是有效的CYarp请求");
                return Results.BadRequest();
            }

            if (cyarpFeature.IsCYarpRequest == false || context.Request.Headers.TryGetValue(CYarpTargetUriHeader, out var targetUri) == false)
            {
                Log.LogInvalidRequest(logger, context.Connection.Id, $"请求头{CYarpTargetUriHeader}不存在");
                return Results.BadRequest();
            }

            // CYarp-TargetUri头格式验证
            if (Uri.TryCreate(targetUri, UriKind.Absolute, out var clientTargetUri) == false)
            {
                Log.LogInvalidRequest(logger, context.Connection.Id, $"请求头{CYarpTargetUriHeader}的值不是Uri格式");
                return Results.BadRequest();
            }

            // 查找clientId
            var clientId = await this.clientIdProvider.Invoke(context);
            if (string.IsNullOrEmpty(clientId))
            {
                Log.LogInvalidRequest(logger, context.Connection.Id, "无法获取到IClient的Id");
                return Results.Forbid();
            }

            var options = yarpOptions.CurrentValue;
            var stream = await cyarpFeature.AcceptAsSafeWriteStreamAsync();
            var connection = new ClientConnection(clientId, stream, options.Connection, logger);

            var disconnected = false;
            await using (var client = new Client(connection, httpForwarder, options.HttpTunnel, httpTunnelFactory, clientTargetUri, context))
            {
                if (await clientManager.AddAsync(client, default))
                {
                    Log.LogConnected(logger, clientId, cyarpFeature.Protocol, clientManager.Count);
                    await connection.WaitForCloseAsync();
                    disconnected = await clientManager.RemoveAsync(client, default);
                }
            }

            if (disconnected)
            {
                Log.LogDisconnected(logger, clientId, cyarpFeature.Protocol, clientManager.Count);
            }

            // 关闭连接
            context.Abort();
            return Results.Empty;
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "连接{connectionId}请求无效：{message}")]
            public static partial void LogInvalidRequest(ILogger logger, string connectionId, string message);

            [LoggerMessage(LogLevel.Information, "[{clientId}] {protocol}长连接成功，系统当前客户端总数为{count}")]
            public static partial void LogConnected(ILogger logger, string clientId, TransportProtocol protocol, int count);

            [LoggerMessage(LogLevel.Warning, "[{clientId}] {protocol}长连接断开，系统当前客户端总数为{count}")]
            public static partial void LogDisconnected(ILogger logger, string clientId, TransportProtocol protocol, int count);
        }
    }
}
