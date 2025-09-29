using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// HTTP/1.1 upgrade client background service
    /// </summary>
    sealed class DefaultClient : ClientBase
    {
        public DefaultClient(
           IOptionsMonitor<CYarpClientOptions> clientOptions,
           ILogger<DefaultClient> logger)
          : base(clientOptions, logger)
        {
        }
    }
}
