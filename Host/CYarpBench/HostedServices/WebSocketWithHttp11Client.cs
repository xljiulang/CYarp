using CYarp.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CYarpBench
{
    /// <summary>
    /// WebSocket的客户端后台服务
    /// </summary>
    sealed class WebSocketWithHttp11Client : ClientHostedService
    {
        public WebSocketWithHttp11Client(IOptionsMonitor<CYarpClientOptions> clientOptions)
           : base(clientOptions, NullLogger<WebSocketWithHttp11Client>.Instance)
        {
        }
    }
}
