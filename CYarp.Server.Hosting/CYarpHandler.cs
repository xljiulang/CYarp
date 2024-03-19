using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CYarp.Server.Hosting
{
    sealed partial class CYarpHandler
    {
        private const string clientIdHeaderName = "Client-Id";

        public static async Task<IResult> InvokeAsync(
            HttpContext httpContext,
            IClientManager clientManager,
            ILogger<CYarpHandler> logger)
        {
            if (httpContext.Request.Headers.TryGetValue(clientIdHeaderName, out var clientIdValues))
            {
                var clientId = clientIdValues.ToString();
                if (clientId != null && clientManager.TryGetValue(clientId, out var client))
                {
                    var stopwatch = Stopwatch.StartNew();

                    httpContext.Request.Headers.Remove(clientIdHeaderName);
                    await client.ForwardHttpAsync(httpContext);
                    stopwatch.Stop();

                    var destination = new Uri(client.Destination, httpContext.Request.Path);
                    Log.ForwardHttpRequest(logger, clientId, httpContext.Request.Method, destination, httpContext.Response.StatusCode, stopwatch.Elapsed.TotalMilliseconds);
                    return Results.Empty;
                }
            }

            return Results.StatusCode(StatusCodes.Status502BadGateway);
        }


        static partial class Log
        {

            [LoggerMessage(LogLevel.Information, "[{clientId}] {method} {destination} response {statusCode} elapsed {elapsed} ms")]
            public static partial void ForwardHttpRequest(ILogger logger, string clientId, string method, Uri destination, int statusCode, double elapsed);
        }
    }
}