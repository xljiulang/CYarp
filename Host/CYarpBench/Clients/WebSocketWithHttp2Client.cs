using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// WebSocket over HTTP/2 client background service
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
