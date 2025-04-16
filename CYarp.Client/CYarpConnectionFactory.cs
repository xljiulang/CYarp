using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    sealed partial class CYarpConnectionFactory : IDisposable
    {
        private const string CYarp = "CYarp";
        private const string CYarpTargetUri = "CYarp-TargetUri";

        private bool? serverHttp2Supported;
        private readonly ILogger logger;
        private readonly CYarpClientOptions options;
        private readonly HttpMessageInvoker httpClient;

        public bool ServerHttp2Supported => serverHttp2Supported == true;

        public CYarpConnectionFactory(
            ILogger logger,
            CYarpClientOptions options,
            HttpMessageHandler handler,
            bool disposeHandler = true)
        {
            this.logger = logger;
            this.options = options;
            this.httpClient = new HttpMessageInvoker(new FactoryHttpHandler(this, handler), disposeHandler);
        }

        /// <summary>
        /// 创建到目的地的通道
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<Stream> CreateTargetTunnelAsync(CancellationToken cancellationToken)
        {
            EndPoint endPoint = string.IsNullOrEmpty(this.options.TargetUnixDomainSocket)
                ? new DnsEndPoint(this.options.TargetUri.Host, this.options.TargetUri.Port)
                : new UnixDomainSocketEndPoint(this.options.TargetUnixDomainSocket);

            var socket = endPoint is DnsEndPoint
                ? new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true }
                : new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(this.options.ConnectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);
                await socket.ConnectAsync(endPoint, linkedTokenSource.Token);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                socket.Dispose();
                throw;
            }
            catch (OperationCanceledException ex)
            {
                socket.Dispose();
                throw new CYarpConnectException(CYarpConnectError.Timedout, ex);
            }
            catch (Exception ex)
            {
                socket.Dispose();
                throw new CYarpConnectException(CYarpConnectError.Failure, ex);
            }
        }

        /// <summary>
        /// 创建到服务器的通道
        /// </summary> 
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<Stream> CreateServerTunnelAsync(Guid tunnelId, CancellationToken cancellationToken)
        {
            var stream = await this.ConnectServerAsync(tunnelId, cancellationToken);
            return new ServerTunnelStream(tunnelId, stream);
        }

        /// <summary>
        /// 创建与CYarp服务器的连接
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<CYarpConnection> CreateServerConnectionAsync(CancellationToken cancellationToken)
        {
            var stream = await this.ConnectServerAsync(tunnelId: null, cancellationToken);
            var safeWriteStream = new SafeWriteStream(stream);
            return new CYarpConnection(safeWriteStream, this.options.KeepAliveInterval, this.logger);
        }


        private Task<Stream> ConnectServerAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            return this.options.ServerUri.Scheme.StartsWith(Uri.UriSchemeWs)
                ? this.WebSocketConnectServerAsync(tunnelId, cancellationToken)
                : this.HttpConnectServerAsync(tunnelId, cancellationToken);
        }

        private async Task<Stream> WebSocketConnectServerAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            var webSocket = new ClientWebSocket();
            webSocket.Options.AddSubProtocol(CYarp);

            if (this.serverHttp2Supported != false && this.options.ServerUri.Scheme == Uri.UriSchemeWss)
            {
                webSocket.Options.HttpVersion = HttpVersion.Version20;
                webSocket.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
            }
            else
            {
                webSocket.Options.HttpVersion = HttpVersion.Version11;
                webSocket.Options.HttpVersionPolicy = HttpVersionPolicy.RequestVersionExact;
            }

            if (tunnelId == null)
            {
                var targetUri = this.options.TargetUri.ToString();
                webSocket.Options.SetRequestHeader(CYarpTargetUri, targetUri);
                foreach (var header in this.options.ConnectHeaders)
                {
                    webSocket.Options.SetRequestHeader(header.Key, header.Value);
                }
            }

            try
            {
                var serverUri = this.CreateServerUri(tunnelId);
                await webSocket.ConnectAsync(serverUri, this.httpClient, cancellationToken);
                return new WebSocketStream(webSocket);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                webSocket.Dispose();
                throw;
            }
            catch (OperationCanceledException ex)
            {
                webSocket.Dispose();
                throw new CYarpConnectException(CYarpConnectError.Timedout, ex);
            }
            catch (WebSocketException ex) when (ex.InnerException is CYarpConnectException connectException)
            {
                webSocket.Dispose();
                throw connectException;
            }
            catch (Exception ex)
            {
                webSocket.Dispose();
                throw new CYarpConnectException(CYarpConnectError.Failure, ex);
            }
        }


        private async Task<Stream> HttpConnectServerAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            try
            {
                return await this.HttpConnectServerCoreAsync(tunnelId, cancellationToken);
            }
            catch (OperationCanceledException ex)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new CYarpConnectException(CYarpConnectError.Timedout, ex);
            }
            catch (CYarpConnectException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new CYarpConnectException(CYarpConnectError.Failure, ex);
            }
        }

        private async Task<Stream> HttpConnectServerCoreAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            if (this.serverHttp2Supported != false && this.options.ServerUri.Scheme == Uri.UriSchemeHttps)
            {
                try
                {
                    return await this.Http20ConnectServerAsync(tunnelId, cancellationToken);
                }
                catch (CYarpConnectException ex) when (ex.ErrorCode >= CYarpConnectError.Unauthorized)
                {
                    throw;
                }
                catch (Exception)
                {
                    // 捕获剩余的其它所有异常，从而降级到http/1.1的Upgrade协议
                }
            }
            return await this.Http11ConnectServerAsync(tunnelId, cancellationToken);
        }

        private async Task<Stream> Http20ConnectServerAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = this.CreateServerUri(tunnelId);
            var request = new HttpRequestMessage(HttpMethod.Connect, serverUri);
            request.Headers.Protocol = CYarp;
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            if (tunnelId == null)
            {
                this.SetAuthorization(request);
            }

            using var timeoutTokenSource = new CancellationTokenSource(this.options.ConnectTimeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            var httpResponse = await this.httpClient.SendAsync(request, linkedTokenSource.Token);
            if (httpResponse.StatusCode != HttpStatusCode.OK)
            {
                var inner = new HttpRequestException(httpResponse.ReasonPhrase, null, httpResponse.StatusCode);
                throw new CYarpConnectException(CYarpConnectError.Failure, inner);
            }
            return await httpResponse.Content.ReadAsStreamAsync(linkedTokenSource.Token);
        }

        private async Task<Stream> Http11ConnectServerAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = this.CreateServerUri(tunnelId);
            var request = new HttpRequestMessage(HttpMethod.Get, serverUri);
            request.Headers.Connection.TryParseAdd("Upgrade");
            request.Headers.Upgrade.TryParseAdd(CYarp);
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            if (tunnelId == null)
            {
                this.SetAuthorization(request);
            }

            using var timeoutTokenSource = new CancellationTokenSource(this.options.ConnectTimeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            var httpResponse = await this.httpClient.SendAsync(request, linkedTokenSource.Token);
            if (httpResponse.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                var inner = new HttpRequestException(httpResponse.ReasonPhrase, null, httpResponse.StatusCode);
                throw new CYarpConnectException(CYarpConnectError.Failure, inner);
            }

            return await httpResponse.Content.ReadAsStreamAsync(linkedTokenSource.Token);
        }

        private void SetAuthorization(HttpRequestMessage request)
        {
            var targetUri = this.options.TargetUri.ToString();
            request.Headers.TryAddWithoutValidation(CYarpTargetUri, targetUri);
            foreach (var header in this.options.ConnectHeaders)
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        private Uri CreateServerUri(Guid? tunnelId)
        {
            return tunnelId == null
                ? new Uri(this.options.ServerUri, "/cyarp")
                : new Uri(this.options.ServerUri, $"/cyarp/{tunnelId}");
        }

        public void Dispose()
        {
            this.httpClient.Dispose();
        }
    }
}
