using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// http/1.1升级Client后台服务
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
