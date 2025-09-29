using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// HTTP/1.1 upgrade client background service
    /// </summary>
    sealed class Http11Client : ClientBase
    {
        public Http11Client(
           IOptionsMonitor<CYarpClientOptions> clientOptions,
           ILogger<Http11Client> logger)
          : base(clientOptions, logger)
        {
        }
    }
}
