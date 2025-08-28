using CYarpGateway.StateStrorages;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarpGateway
{
    /// <summary>
    /// httpForwardMiddleware
    /// </summary>
    sealed partial class HttpForwardMiddleware : IMiddleware
    {
        private readonly RedisClientStateStorage stateStorage;
        private readonly IHttpForwarder httpForwarder;
        private readonly ILogger<HttpForwardMiddleware> logger;
        private readonly HttpMessageInvoker httpClient;
        private static readonly string clientIdClaimType = "ClientId";

        public HttpForwardMiddleware(
            RedisClientStateStorage stateStorage,
            IHttpForwarder httpForwarder,
            ILogger<HttpForwardMiddleware> logger)
        {
            this.stateStorage = stateStorage;
            this.httpForwarder = httpForwarder;
            this.logger = logger;
            this.httpClient = new HttpMessageInvoker(CreatePrimitiveHandler());
        }

        /// <summary>
        /// ForwardhttpRequestToclientId对应CYarpServer节点
        /// </summary>
        /// <param name="context"></param>
        /// <param name="next"></param>
        /// <returns></returns>
        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var clientId = context.User.FindFirstValue(clientIdClaimType);
            if (clientId == null)
            {
                Log.LogClientIdNotFound(this.logger, context.Connection.Id);
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            var destination = await this.stateStorage.GetNodeDestinationAsync(clientId);
            if (destination == null)
            {
                Log.LogNodeNotFound(this.logger, clientId);
                context.Response.StatusCode = StatusCodes.Status502BadGateway;
                return;
            }

            await this.httpForwarder.SendAsync(context, destination, this.httpClient, ForwarderRequestConfig.Empty, HttpTransformer.Empty);
        }

        private static SocketsHttpHandler CreatePrimitiveHandler()
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = null,
                UseProxy = false,
                UseCookies = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.None,
                RequestHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ResponseHeaderEncodingSelector = (header, context) => Encoding.UTF8,
                ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
            };

            return handler;
        }

        static partial class Log
        {
            [LoggerMessage(LogLevel.Warning, "[{connection}] 匹配不ToClientId")]
            public static partial void LogClientIdNotFound(ILogger logger, string connection);

            [LoggerMessage(LogLevel.Warning, "[{clientId}] 找不ToAssociatedNode")]
            public static partial void LogNodeNotFound(ILogger logger, string clientId);
        }
    }
}
