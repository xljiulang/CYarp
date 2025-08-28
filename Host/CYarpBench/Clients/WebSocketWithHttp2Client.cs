using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// WebSocket over Http2Client后台服务
    /// </summary>
    sealed class WebSocketWithHttp2Client : ClientBase
    {
        public WebSocketWithHttp2Client(
            IOptionsMonitor<CYarpClientOptions> clientOptions,
            ILogger<WebSocketWithHttp2Client> logger)
           : base(clientOptions, logger)
        {
        }
    }
}
