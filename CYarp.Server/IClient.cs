using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Net;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server
{
    /// <summary>
    /// Client interface
    /// </summary>
    public interface IClient : IAsyncDisposable
    {
        /// <summary>
        /// Get unique identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Get forwarding target Uri
        /// </summary>
        Uri TargetUri { get; }

        /// <summary>
        /// Get associated user information
        /// </summary>
        ClaimsPrincipal User { get; }

        /// <summary>
        /// Get transport protocol
        /// </summary>
        TransportProtocol Protocol { get; }

        /// <summary>
        /// Get remote endpoint
        /// </summary>
        IPEndPoint? RemoteEndpoint { get; }

        /// <summary>
        /// Get number of still connected TcpTunnels
        /// </summary>
        int TcpTunnelCount { get; }

        /// <summary>
        /// Get number of still connected HttpTunnels
        /// </summary>
        int HttpTunnelCount { get; }

        /// <summary>
        /// Get creation time
        /// </summary>
        DateTimeOffset CreationTime { get; }

        /// <summary>
        /// Create a transport tunnel that can carry TCP streams to the EndPoint of <see cref="TargetUri"/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Stream> CreateTcpTunnelAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Forward HTTP request to associated <see cref="TargetUri"/>
        /// </summary>
        /// <param name="context">HTTP context</param> 
        /// <param name="transformer">HTTP content transformer</param>
        /// <returns></returns>
        ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, HttpTransformer? transformer = default);
    }
}
