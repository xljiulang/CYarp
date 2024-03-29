using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp客户端
    /// </summary>
    public class CYarpClient : IDisposable
    {
        private readonly CYarpClientOptions options;
        private readonly CYarpConnectionFactory connectionFactory;
        private readonly CancellationTokenSource disposeTokenSource = new();

        /// <summary>
        /// CYarp客户端
        /// </summary>
        /// <param name="options">客户端选项</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(CYarpClientOptions options)
            : this(options, new HttpClientHandler())
        {
        }

        /// <summary>
        /// CYarp客户端
        /// </summary>
        /// <param name="options">客户端选项</param> 
        /// <param name="handler">httpHandler</param>
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
            var httpClient = new HttpMessageInvoker(new UnauthorizedHttpHandler(handler), disposeHandler);
            this.connectionFactory = new CYarpConnectionFactory(httpClient, options);
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
            ObjectDisposedException.ThrowIf(this.disposeTokenSource.IsCancellationRequested, this);

            var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disposeTokenSource.Token);
            await this.TransportCoreAsync(linkedTokenSource.Token);
        }

        /// <summary>
        /// 连接到CYarp服务器并开始隧道传输
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="CYarpConnectException"></exception> 
        /// <exception cref="OperationCanceledException"></exception>
        private async Task TransportCoreAsync(CancellationToken cancellationToken)
        {
            var stream = await this.connectionFactory.ConnectServerAsync(cancellationToken);
            await using var connection = new CYarpConnection(stream, this.options.KeepAliveInterval);
            using var connectionTokenSource = new CancellationTokenSource();

            try
            {
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connectionTokenSource.Token);
                await foreach (var tunnelId in connection.ReadTunnelIdAsync(cancellationToken))
                {
                    this.BindTunnelIOAsync(tunnelId, linkedTokenSource.Token);
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                connectionTokenSource.Cancel();
            }
        }

        /// <summary>
        /// 绑定tunnel的IO
        /// </summary> 
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        private async void BindTunnelIOAsync(Guid tunnelId, CancellationToken cancellationToken)
        {
            try
            {
                await using var targetTunnel = await this.connectionFactory.CreateTargetTunnelAsync(cancellationToken);
                await using var serverTunnel = await this.connectionFactory.CreateServerTunnelAsync(tunnelId, cancellationToken);

                var server2Target = serverTunnel.CopyToAsync(targetTunnel, cancellationToken);
                var target2Server = targetTunnel.CopyToAsync(serverTunnel, cancellationToken);
                await Task.WhenAny(server2Target, target2Server);
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
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (this.disposeTokenSource.IsCancellationRequested == false)
            {
                this.disposeTokenSource.Cancel();
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
            this.connectionFactory.Dispose();
            this.disposeTokenSource.Dispose();
        }

        private class UnauthorizedHttpHandler : DelegatingHandler
        {
            public UnauthorizedHttpHandler(HttpMessageHandler innerHandler)
            {
                this.InnerHandler = innerHandler;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                var httpResponse = await base.SendAsync(request, cancellationToken);
                if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var inner = new HttpRequestException(httpResponse.ReasonPhrase, null, httpResponse.StatusCode);
                    throw new CYarpConnectException(CYarpConnectError.Unauthorized, inner);
                }
                return httpResponse;
            }
        }
    }
}
