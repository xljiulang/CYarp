using CYarp.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CYarpBench
{
    /// <summary>
    /// http/1.1升级的客户端后台服务
    /// </summary>
    sealed class Http11Client : ClientHostedService
    {
        public Http11Client(IOptionsMonitor<CYarpClientOptions> clientOptions)
            : base(clientOptions, NullLogger<Http11Client>.Instance)
        {
        }
    }
}
