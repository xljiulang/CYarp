using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// WebSocket client background service
    /// </summary>
    sealed class WebSocketWithHttp11Client : ClientBase
    {
        public WebSocketWithHttp11Client(
              IOptionsMonitor<CYarpClientOptions> clientOptions,
              ILogger<WebSocketWithHttp11Client> logger)
             : base(clientOptions, logger)
        {
        }
    }
}
