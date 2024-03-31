using CYarp.Client;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CYarpBench
{
    /// <summary>
    /// HTTP/2 Extended CONNECT的客户端后台服务
    /// </summary>
    sealed class Http2Client : ClientHostedService
    {
        public Http2Client(IOptionsMonitor<CYarpClientOptions> clientOptions)
           : base(clientOptions, NullLogger<Http2Client>.Instance)
        {
        }
    }
}
