using CYarp.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CYarpBench.Clients
{
    /// <summary>
    /// http/1.1升级Client后台服务
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
