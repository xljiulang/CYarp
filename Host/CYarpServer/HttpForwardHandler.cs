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
    public partial class HttpForwardHandler
    {
        private static readonly string clientIdClaimType = "ClientId";

        /// <summary>
        /// Handle CYarp forwarding
        /// The core operation is to obtain the clientId from the request context
        /// then use the clientId to get an IClient from the IClientViewer service to forward the HTTP request
        /// </summary>
        /// <param name="clientViewer">Viewer for IClient</param>
        /// <param name="context"></param>
        /// <param name="logger"></param>
        /// <returns></returns> 
        public static async Task HandleAsync(HttpContext context, IClientViewer clientViewer, ILogger<Program> logger)
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

            [LoggerMessage(LogLevel.Information, "<= [{clientId}] {method} {uri} {statusCode}, elapsed {elapsed}")]
            public static partial void LogResponse(ILogger logger, string clientId, string method, Uri uri, int statusCode, TimeSpan elapsed);
        }
    }
}
