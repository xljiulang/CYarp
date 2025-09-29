using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// HTTP/2 Extended CONNECT client background service
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
