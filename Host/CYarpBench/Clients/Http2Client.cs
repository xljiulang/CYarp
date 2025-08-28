using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// HTTP/2 Extended CONNECTClient后台服务
    /// </summary>
    sealed class Http2Client : ClientBase
    {
        public Http2Client(
            IOptionsMonitor<CYarpClientOptions> clientOptions,
            ILogger<Http2Client> logger)
           : base(clientOptions, logger)
        {
        }
    }
}
