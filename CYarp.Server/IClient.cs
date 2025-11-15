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
        /// Gets the unique identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Gets the destination Uri for forwarding
        /// </summary>
        Uri TargetUri { get; }

        /// <summary>
        /// Gets the associated user principal
        /// </summary>
        ClaimsPrincipal User { get; }

        /// <summary>
        /// Gets the transport protocol
        /// </summary>
        TransportProtocol Protocol { get; }

        /// <summary>
        /// Gets the remote endpoint
        /// </summary>
        IPEndPoint? RemoteEndpoint { get; }

        /// <summary>
        /// Gets the current connected TCP tunnel count
        /// </summary>
        int TcpTunnelCount { get; }

        /// <summary>
        /// Gets the current connected HTTP tunnel count
        /// </summary>
        int HttpTunnelCount { get; }

        /// <summary>
        /// Gets the creation time
        /// </summary>
        DateTimeOffset CreationTime { get; }

        /// <summary>
        /// Create a transport tunnel that carries TCP stream to the EndPoint of <see cref="TargetUri"/>
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<Stream> CreateTcpTunnelAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Forward the http request to the associated <see cref="TargetUri"/>
        /// </summary>
        /// <param name="context">HttpContext</param> 
        /// <param name="transformer">Http content transformer</param>
        /// <returns></returns>
        ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, HttpTransformer? transformer = default);
    }
}
