using CYarp.Server;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarpServer
{
    public partial class CYarpHandler
    {
        private static readonly string clientIdClaimType = "ClientId";

        /// <summary>
        /// 处理cyarp
        /// 核心操作是从请求上下文获取clientId
        /// 然后使用clientId从IClientViewer服务获取IClient来转发http
        /// </summary>
        /// <param name="clientViewer">IClient的查看器</param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <returns></returns> 
        public static async Task HandleCYarpAsync(HttpContext context, IClientViewer clientViewer, ILogger<Program> logger)
        {
            var clientId = context.User.FindFirstValue(clientIdClaimType);
            if (clientId != null && clientViewer.TryGetValue(clientId, out var client))
            {
                var stopwatch = Stopwatch.StartNew();
                var uri = new Uri(client.TargetUri, context.Request.Path);
                Log.LogRequest(logger, clientId, context.Request.Method, uri);

                context.Request.Headers.Remove(HeaderNames.Authorization);
                var error = await client.ForwardHttpAsync(context);

                stopwatch.Stop();
                if (error == Yarp.ReverseProxy.Forwarder.ForwarderError.None)
                {
                    Log.LogResponse(logger, clientId, context.Request.Method, uri, context.Response.StatusCode, stopwatch.Elapsed);
                }
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
            }
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "=> [{clientId}] {method} {uri}")]
            public static partial void LogRequest(ILogger logger, string clientId, string method, Uri uri);

            [LoggerMessage(LogLevel.Information, "<= [{clientId}] {method} {uri} {statusCode}，过程耗时{elapsed}")]
            public static partial void LogResponse(ILogger logger, string clientId, string method, Uri uri, int statusCode, TimeSpan elapsed);
        }
    }
}
