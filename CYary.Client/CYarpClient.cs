using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CYary.Client
{
    /// <summary>
    /// CYarp客户端
    /// </summary>
    public class CYarpClient : IDisposable
    {
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
            options.Validate();
            using var serverStream = await this.ConnectCYarpServerAsync(options, tunnelId: null, cancellationToken);
            var streamReader = new StreamReader(serverStream, leaveOpen: true);

            while (streamReader.EndOfStream == false)
            {
                var tunnelId = await streamReader.ReadLineAsync(cancellationToken);
                if (string.IsNullOrEmpty(tunnelId))
                {
                    break;
                }
                else
                {
                    this.TransportAsync(options, tunnelId, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 发起隧道传输
        /// </summary>
        /// <param name="options"></param>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        private async void TransportAsync(CYarpClientOptions options, string tunnelId, CancellationToken cancellationToken)
        {
            try
            {
                var tunnelUri = new Uri(options.CYarpServer, $"/{tunnelId}");
                using var destinationStream = await ConnectDestinationAsync(options, cancellationToken);
                using var tunnelStream = await this.ConnectCYarpServerAsync(options, tunnelId, cancellationToken);

                var task1 = tunnelStream.CopyToAsync(destinationStream, cancellationToken);
                var task2 = destinationStream.CopyToAsync(tunnelStream, cancellationToken);
                await Task.WhenAny(task1, task2);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                this.OnTunnelException(ex);
            }
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
        private static async Task<Stream> ConnectDestinationAsync(CYarpClientOptions options, CancellationToken cancellationToken)
        {
            var destination = options.Destination;
            var socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            await socket.ConnectAsync(destination.Host, destination.Port, cancellationToken);
            return new NetworkStream(socket);
        }

        /// <summary>
        /// 创建与CYarp服务器的连接
        /// </summary>
        /// <param name="options"></param>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<CYarpClientStream> ConnectCYarpServerAsync(CYarpClientOptions options, string? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = options.CYarpServer;
            if (serverUri.Scheme == Uri.UriSchemeHttps)
            {
                try
                {
                    return await this.HttpConnectAsync(options, tunnelId, cancellationToken);
                }
                catch (HttpRequestException)
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
            var serverUri = new Uri(options.CYarpServer, $"/{tunnelId}");
            var request = new HttpRequestMessage(HttpMethod.Connect, serverUri);
            request.Headers.Protocol = "CYarp";

            if (string.IsNullOrEmpty(tunnelId))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(options.Authorization);
                request.Headers.TryAddWithoutValidation("CYarp-Destination", options.Destination.OriginalString);
            }

            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

            var httpResponse = await this.httpClient.SendAsync(request, cancellationToken);
            var stream = await httpResponse.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(cancellationToken);
            return new CYarpClientStream(stream);
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
            var serverUri = new Uri(options.CYarpServer, $"/{tunnelId}");
            var request = new HttpRequestMessage(HttpMethod.Get, serverUri);
            request.Headers.Connection.TryParseAdd("Upgrade");
            request.Headers.Upgrade.TryParseAdd("CYarp");

            if (string.IsNullOrEmpty(tunnelId))
            {
                request.Headers.Authorization = AuthenticationHeaderValue.Parse(options.Authorization);
                request.Headers.TryAddWithoutValidation("CYarp-Destination", options.Destination.OriginalString);
            }

            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            var httpResponse = await this.httpClient.SendAsync(request, cancellationToken);
            if (httpResponse.StatusCode != HttpStatusCode.SwitchingProtocols)
            {
                throw new HttpRequestException(httpResponse.ReasonPhrase);
            }

            var stream = await httpResponse.Content.ReadAsStreamAsync(cancellationToken);
            return new CYarpClientStream(stream);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public virtual void Dispose()
        {
            this.httpClient.Dispose();
        }
    }
}
