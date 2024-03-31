using CYarp.Server;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarpServer
{
    [Authorize(Roles = "Mobile")]
    public partial class CYarpController : ControllerBase
    {
        private static readonly string clientIdClaimType = "ClientId";
        private readonly ILogger<CYarpController> logger;

        public CYarpController(ILogger<CYarpController> logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// 处理cyarp
        /// 核心操作是从请求上下文获取clientId
        /// 然后使用clientId从IClientViewer服务获取IClient来转发http
        /// </summary>
        /// <param name="clientViewer">IClient的查看器</param>
        /// <returns></returns>
        [Route("/{**cyarp}")]
        public async Task InvokeAsync([FromServices] IClientViewer clientViewer)
        {
            var clientId = this.User.FindFirstValue(clientIdClaimType);
            if (clientId != null && clientViewer.TryGetValue(clientId, out var client))
            {
                var stopwatch = Stopwatch.StartNew();
                var uri = new Uri(client.TargetUri, Request.Path);
                Log.LogRequest(this.logger, clientId, Request.Method, uri);

                this.Request.Headers.Remove(HeaderNames.Authorization);
                var error = await client.ForwardHttpAsync(this.HttpContext);
               
                stopwatch.Stop();
                if (error == Yarp.ReverseProxy.Forwarder.ForwarderError.None)
                {
                    Log.LogResponse(this.logger, clientId, Request.Method, uri, Response.StatusCode, stopwatch.Elapsed);
                }
            }
            else
            {
                this.Response.StatusCode = StatusCodes.Status502BadGateway;
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
