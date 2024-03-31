using CYarp.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CYarpBench
{
    /// <summary>
    /// WebSocket over Http2的客户端后台服务
    /// </summary>
    sealed class WebSocketWithHttp2Client : ClientHostedService
    {
        public WebSocketWithHttp2Client(IOptionsMonitor<CYarpClientOptions> clientOptions)
           : base(clientOptions, NullLogger<WebSocketWithHttp2Client>.Instance)
        {
        }
    }
}
