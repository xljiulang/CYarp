using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp客户端
    /// </summary>
    public class CYarpClient : IDisposable
    {
        private volatile bool disposed = false;
        private readonly HttpMessageInvoker httpClient;

        /// <summary>
        /// CYarp客户端
        /// </summary> 
        public CYarpClient()
            : this(new HttpClientHandler())
        {
        }

        /// <summary>
        /// CYarp客户端
        /// </summary> 
        /// <param name="handler"></param>
        /// <param name="disposeHandler"></param>
        public CYarpClient(
            HttpMessageHandler handler,
            bool disposeHandler = true)
        {
            this.httpClient = new HttpMessageInvoker(handler, disposeHandler);
        }

        /// <summary>
        /// 连接到CYarp服务器并开始隧道传输
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task TransportAsync(CYarpClientOptions options, CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);
            options.Validate();

            using var serverStream = await this.ConnectServerAsync(options, tunnelId: null, cancellationToken);
            using var streamReader = new StreamReader(serverStream, leaveOpen: true);

            while (streamReader.EndOfStream == false)
            {
                var tunnelId = await streamReader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(tunnelId))
                {
                    break;
                }
                else
                {
                    this.TunnelAsync(options, tunnelId, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 发起隧道传输
        /// </summary>
        /// <param name="options"></param>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        private async void TunnelAsync(CYarpClientOptions options, string tunnelId, CancellationToken cancellationToken)
        {
            try
            {
                using var targetStream = await ConnectTargetAsync(options, cancellationToken);
                using var serverStream = await this.ConnectServerAsync(options, tunnelId, cancellationToken);

                var task1 = serverStream.CopyToAsync(targetStream, cancellationToken);
                var task2 = targetStream.CopyToAsync(serverStream, cancellationToken);
                await Task.WhenAny(task1, task2);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                this.OnTunnelExceptionCore(options, ex);
            }
        }

        /// <summary>
        /// 隧道异常时
        /// </summary>
        /// <param name="options"></param>
        /// <param name="ex"></param>
        private unsafe void OnTunnelExceptionCore(CYarpClientOptions options, Exception ex)
        {
            this.OnTunnelException(ex);
            options.TunnelErrorCallback?.Invoke(ex);
        }

        /// <summary>
        /// 隧道异常时
        /// </summary>
        /// <param name="ex"></param>
        protected virtual void OnTunnelException(Exception ex)
        {
        }

        /// <summary>
        /// 连接到目的地
        /// </summary>
        /// <param name="options"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private static async Task<Stream> ConnectTargetAsync(CYarpClientOptions options, CancellationToken cancellationToken)
        {
            EndPoint endPoint = string.IsNullOrEmpty(options.TargetUnixDomainSocket)
                ? new DnsEndPoint(options.TargetUri.Host, options.TargetUri.Port)
                : new UnixDomainSocketEndPoint(options.TargetUnixDomainSocket);

            var socket = endPoint is DnsEndPoint
                ? new Socket(SocketType.Stream, ProtocolType.Tcp)
                : new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(options.ConnectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);
                await socket.ConnectAsync(endPoint, linkedTokenSource.Token);
                return new NetworkStream(socket);
            }
            catch (Exception)
            {
                socket.Dispose();
                throw;
            }
        }

        /// <summary>
        /// 创建与CYarp服务器的连接
        /// </summary>
        /// <param name="options"></param>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<CYarpClientStream> ConnectServerAsync(CYarpClientOptions options, string? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = options.ServerUri;
            if (serverUri.Scheme == Uri.UriSchemeHttps)
            {
                try
                {
                    return await this.HttpConnectAsync(options, tunnelId, cancellationToken);
                }
                catch (HttpRequestException ex) when (ex.StatusCode != HttpStatusCode.Unauthorized)
                {
                    return await this.HttpUpgradesync(options, tunnelId, cancellationToken);
                }
            }
            else
            {
                return await this.HttpUpgradesync(options, tunnelId, cancellationToken);
            }
        }

        /// <summary>
        /// 使用http/2.0的Connect扩展协议升级连接为长连接
        /// </summary>
        /// <param name="options"></param>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<CYarpClientStream> HttpConnectAsync(CYarpClientOptions options, string? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = new Uri(options.ServerUri, $"/{tunnelId}");
            var request = new HttpRequestMessage(HttpMethod.Connect, serverUri);
            request.Headers.Protocol = "CYarp";
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

            if (string.IsNullOrEmpty(tunnelId))
            {
                SetAuthorization(request, options);
            }

            using var timeoutTokenSource = new CancellationTokenSource(options.ConnectTimeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            var httpResponse = await this.httpClient.SendAsync(request, linkedTokenSource.Token);
            var stream = await httpResponse.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(linkedTokenSource.Token);
            return new CYarpClientStream(stream, httpResponse.Version);
        }

        /// <summary>
        /// 使用http/1.1的Upgrade扩展协议升级连接为长连接
        /// </summary>
        /// <param name="options"></param>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        private async Task<CYarpClientStream> HttpUpgradesync(CYarpClientOptions options, string? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = new Uri(options.ServerUri, $"/{tunnelId}");
            var request = new HttpRequestMessage(HttpMethod.Get, serverUri);
            request.Headers.Connection.TryParseAdd("Upgrade");
            request.Headers.Upgrade.TryParseAdd("CYarp");
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            if (string.IsNullOrEmpty(tunnelId))
            {
                SetAuthorization(request, options);
            }

            using var timeoutTokenSource = new CancellationTokenSource(options.ConnectTimeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            var httpResponse = await this.httpClient.SendAsync(request, linkedTokenSource.Token);
            if (httpResponse.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                throw new HttpRequestException(httpResponse.ReasonPhrase, null, httpResponse.StatusCode);
            }

            var stream = await httpResponse.Content.ReadAsStreamAsync(linkedTokenSource.Token);
            return new CYarpClientStream(stream, httpResponse.Version);
        }

        private static void SetAuthorization(HttpRequestMessage request, CYarpClientOptions options)
        {
            var destination = options.TargetUri.OriginalString;
            request.Headers.TryAddWithoutValidation("CYarp-Destination", destination);
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(options.Authorization);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (this.disposed == false)
            {
                this.disposed = true;
                this.Dispose(disposing: true);
            }
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            this.httpClient.Dispose();
        }
    }
}
