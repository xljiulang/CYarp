using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
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
        private readonly CYarpClientOptions options;
        private readonly HttpMessageInvoker httpClient;
        private readonly CancellationTokenSource disposeTokenSource = new();

        private static readonly string PING = "PING";
        private static readonly ReadOnlyMemory<byte> PONG = "PONG\r\n"u8.ToArray();

        /// <summary>
        /// CYarp客户端
        /// </summary>
        /// <param name="options"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(CYarpClientOptions options)
            : this(options, new HttpClientHandler())
        {
        }

        /// <summary>
        /// CYarp客户端
        /// </summary>
        /// <param name="options"></param> 
        /// <param name="handler"></param>
        /// <param name="disposeHandler"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(
            CYarpClientOptions options,
            HttpMessageHandler handler,
            bool disposeHandler = true)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(handler);
            options.Validate();

            this.options = options;
            this.httpClient = new HttpMessageInvoker(handler, disposeHandler);
        }

        /// <summary>
        /// 连接到CYarp服务器并开始隧道传输
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task TransportAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(this.disposed, this);

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disposeTokenSource.Token);
            await this.TransportAsyncCore(linkedTokenSource.Token);
        }

        /// <summary>
        /// 连接到CYarp服务器并开始隧道传输
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="CYarpConnectException"></exception> 
        /// <exception cref="OperationCanceledException"></exception>
        private async Task TransportAsyncCore(CancellationToken cancellationToken)
        {
            var sninalStream = await this.ConnectServerAsync(tunnelId: null, cancellationToken); ;
            using var signalTunnel = new SignalTunnel(sninalStream);
            using var signalTunnelTokenSource = new CancellationTokenSource();

            try
            {
                using var httpTunnelTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, signalTunnelTokenSource.Token);
                await foreach (var tunnelId in ReadTunnelIdAsync(signalTunnel, cancellationToken))
                {
                    this.DuplexTransportAsync(tunnelId, httpTunnelTokenSource.Token);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                signalTunnelTokenSource.Cancel();
            }
        }

        private static async IAsyncEnumerable<Guid> ReadTunnelIdAsync(SignalTunnel signalTunnel, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var streamReader = new StreamReader(signalTunnel, leaveOpen: true);
            while (cancellationToken.IsCancellationRequested == false)
            {
                var text = await streamReader.ReadLineAsync(cancellationToken);
                if (text == null)
                {
                    yield break;
                }
                else if (text == PING)
                {
                    await signalTunnel.WriteAsync(PONG, cancellationToken);
                }
                else if (Guid.TryParse(text, out var tunnelId))
                {
                    yield return tunnelId;
                }
            }
        }


        /// <summary>
        /// 双向传输绑定
        /// </summary> 
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        private async void DuplexTransportAsync(Guid tunnelId, CancellationToken cancellationToken)
        {
            try
            {
                using var targetTunnel = await this.ConnectTargetAsync(cancellationToken);
                using var httpStream = await this.ConnectServerAsync(tunnelId, cancellationToken);
                using var httpTunnel = new HttpTunnel(httpStream, ownsInner: false);

                var task1 = httpTunnel.CopyToAsync(targetTunnel, cancellationToken);
                var task2 = targetTunnel.CopyToAsync(httpTunnel, cancellationToken);
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
            this.options.TunnelErrorCallback?.Invoke(ex);
        }

        /// <summary>
        /// 连接到目的地
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        private async Task<NetworkStream> ConnectTargetAsync(CancellationToken cancellationToken)
        {
            EndPoint endPoint = string.IsNullOrEmpty(this.options.TargetUnixDomainSocket)
                ? new DnsEndPoint(this.options.TargetUri.Host, this.options.TargetUri.Port)
                : new UnixDomainSocketEndPoint(this.options.TargetUnixDomainSocket);

            var socket = endPoint is DnsEndPoint
                ? new Socket(SocketType.Stream, ProtocolType.Tcp)
                : new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);

            try
            {
                using var timeoutTokenSource = new CancellationTokenSource(this.options.ConnectTimeout);
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);
                await socket.ConnectAsync(endPoint, linkedTokenSource.Token);
                return new NetworkStream(socket);
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
        /// 创建与CYarp服务器的连接
        /// </summary> 
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        private async Task<Stream> ConnectServerAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            try
            {
                if (this.options.ServerUri.Scheme == Uri.UriSchemeHttps)
                {
                    try
                    {
                        return await this.HttpConnectAsync(tunnelId, cancellationToken);
                    }
                    catch (HttpRequestException ex) when (ex.StatusCode != HttpStatusCode.Unauthorized)
                    {
                        // 非Unauthorized状态时，继续尝试http/1.1的Upgrade协议
                    }
                }
                return await this.HttpUpgradesync(tunnelId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException ex)
            {
                throw new CYarpConnectException(CYarpConnectError.Timedout, ex);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new CYarpConnectException(CYarpConnectError.Unauthorized, ex);
            }
            catch (Exception ex)
            {
                throw new CYarpConnectException(CYarpConnectError.Failure, ex);
            }
        }

        /// <summary>
        /// 使用http/2.0的Connect扩展协议升级连接为长连接
        /// </summary>
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<Stream> HttpConnectAsync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = new Uri(this.options.ServerUri, $"/{tunnelId}");
            var request = new HttpRequestMessage(HttpMethod.Connect, serverUri);
            request.Headers.Protocol = "CYarp";
            request.Version = HttpVersion.Version20;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;

            if (tunnelId == null)
            {
                this.SetAuthorization(request);
            }

            using var timeoutTokenSource = new CancellationTokenSource(this.options.ConnectTimeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            var httpResponse = await this.httpClient.SendAsync(request, linkedTokenSource.Token);
            return await httpResponse.EnsureSuccessStatusCode().Content.ReadAsStreamAsync(linkedTokenSource.Token);
        }

        /// <summary>
        /// 使用http/1.1的Upgrade扩展协议升级连接为长连接
        /// </summary> 
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="HttpRequestException"></exception>
        private async Task<Stream> HttpUpgradesync(Guid? tunnelId, CancellationToken cancellationToken)
        {
            var serverUri = new Uri(this.options.ServerUri, $"/{tunnelId}");
            var request = new HttpRequestMessage(HttpMethod.Get, serverUri);
            request.Headers.Connection.TryParseAdd("Upgrade");
            request.Headers.Upgrade.TryParseAdd("CYarp");
            request.Version = HttpVersion.Version11;
            request.VersionPolicy = HttpVersionPolicy.RequestVersionExact;

            if (tunnelId == null)
            {
                this.SetAuthorization(request);
            }

            using var timeoutTokenSource = new CancellationTokenSource(this.options.ConnectTimeout);
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(timeoutTokenSource.Token, cancellationToken);

            var httpResponse = await this.httpClient.SendAsync(request, linkedTokenSource.Token);
            return httpResponse.StatusCode == HttpStatusCode.SwitchingProtocols
                ? await httpResponse.Content.ReadAsStreamAsync(linkedTokenSource.Token)
                : throw new HttpRequestException(httpResponse.ReasonPhrase, null, httpResponse.StatusCode);
        }

        private void SetAuthorization(HttpRequestMessage request)
        {
            var destination = this.options.TargetUri.OriginalString;
            request.Headers.TryAddWithoutValidation("CYarp-Destination", destination);
            request.Headers.Authorization = AuthenticationHeaderValue.Parse(this.options.Authorization);
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
            this.disposeTokenSource.Cancel();
            this.disposeTokenSource.Dispose();
        }
    }
}
