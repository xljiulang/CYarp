using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp客户端
    /// </summary>
    public partial class CYarpClient : IDisposable
    {
        private readonly CYarpClientOptions options;
        private readonly ILogger logger;
        private readonly CYarpConnectionFactory connectionFactory;
        private readonly CancellationTokenSource disposeTokenSource = new();

        /// <summary>
        /// CYarp客户端
        /// </summary>
        /// <param name="options">客户端选项</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(CYarpClientOptions options)
            : this(options, NullLogger<CYarpClient>.Instance)
        {
        }

        /// <summary>
        /// CYarp客户端
        /// </summary>
        /// <param name="options">客户端选项</param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(CYarpClientOptions options, ILogger logger)
            : this(options, logger, new SocketsHttpHandler { EnableMultipleHttp2Connections = true })
        {
        }

        /// <summary>
        /// CYarp客户端
        /// </summary>
        /// <param name="options">客户端选项</param>
        /// <param name="logger">日志组件</param> 
        /// <param name="handler">httpHandler</param>
        /// <param name="disposeHandler"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(
            CYarpClientOptions options,
            ILogger logger,
            HttpMessageHandler handler,
            bool disposeHandler = true)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);
            ArgumentNullException.ThrowIfNull(handler);
            options.Validate();

            this.options = options;
            this.logger = logger;
            this.connectionFactory = new CYarpConnectionFactory(logger, options, handler, disposeHandler);
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
            await using var connection = await this.connectionFactory.CreateServerConnectionAsync(cancellationToken);
            Log.LogConnected(this.logger, this.options.ServerUri);

            using var connectionTokenSource = new CancellationTokenSource();
            try
            {
                using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, connectionTokenSource.Token);
                await foreach (var tunnelId in connection.ReadTunnelIdAsync(cancellationToken))
                {
                    Log.LogTunnelCreating(this.logger, tunnelId);
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
                Log.LogTunnelCreated(this.logger, tunnelId);

                var server2Target = serverTunnel.CopyToAsync(targetTunnel, cancellationToken);
                var target2Server = targetTunnel.CopyToAsync(serverTunnel, cancellationToken);
                var task = await Task.WhenAny(server2Target, target2Server);

                if (task == server2Target)
                {
                    Log.LogTunnelClosed(this.logger, tunnelId, this.options.ServerUri);
                }
                else
                {
                    Log.LogTunnelClosed(this.logger, tunnelId, this.options.TargetUri);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                this.OnTunnelException(ex);
                Log.LogTunnelError(this.logger, tunnelId, ex.Message);
            }
        }

        /// <summary>
        /// 隧道异常时
        /// </summary>
        /// <param name="ex">异常</param>
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


        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "已成功连接到{address}")]
            public static partial void LogConnected(ILogger logger, Uri address);

            [LoggerMessage(LogLevel.Information, "[{tunnelId}] 传输通道正在创建中")]
            public static partial void LogTunnelCreating(ILogger logger, Guid tunnelId);

            [LoggerMessage(LogLevel.Information, "[{tunnelId}] 传输通道已创建完成")]
            public static partial void LogTunnelCreated(ILogger logger, Guid tunnelId);

            [LoggerMessage(LogLevel.Information, "[{tunnelId}] 传输通道已被{address}关闭")]
            public static partial void LogTunnelClosed(ILogger logger, Guid tunnelId, Uri address);

            [LoggerMessage(LogLevel.Warning, "[{tunnelId}] 传输通道异常：{reason}")]
            public static partial void LogTunnelError(ILogger logger, Guid tunnelId, string? reason);
        }
    }
}
