using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client
{
    /// <summary>
    /// CYarp client
    /// </summary>
    public partial class CYarpClient : IDisposable
    {
        private int tunnelCount = 0;
        private readonly CYarpClientOptions options;
        private readonly ILogger logger;
        private readonly CYarpConnectionFactory connectionFactory;
        private readonly CancellationTokenSource disposeTokenSource = new();

        /// <summary>
        /// Get current tunnel count
        /// </summary>
        public int TunnelCount => this.tunnelCount;

        /// <summary>
        /// CYarp client
        /// </summary>
        /// <param name="options">Client options</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(CYarpClientOptions options)
            : this(options, NullLogger<CYarpClient>.Instance)
        {
        }

        /// <summary>
        /// CYarp client
        /// </summary>
        /// <param name="options">Client options</param>
        /// <param name="logger"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(CYarpClientOptions options, ILogger logger)
            : this(options, logger, CreateDefaultHttpHandler())
        {
        }

        /// <summary>
        /// CYarp client
        /// </summary>
        /// <param name="options">Client options</param>
        /// <param name="logger">Logger component</param> 
        /// <param name="handler">httpHandler</param>
        /// <param name="disposeHandler"></param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public CYarpClient(
            CYarpClientOptions options,
            ILogger? logger,
            HttpMessageHandler handler,
            bool disposeHandler = true)
        {
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(handler);
            options.Validate();

            this.options = options;
            this.logger = logger ?? NullLogger<CYarpClient>.Instance;
            this.connectionFactory = new CYarpConnectionFactory(this.logger, options, handler, disposeHandler);
        }

        private static SocketsHttpHandler CreateDefaultHttpHandler()
        {
            return new SocketsHttpHandler
            {
                EnableMultipleHttp2Connections = true
            };
        }

        internal void SetConnectHeader(string? name, string? value)
        {
            if (string.IsNullOrEmpty(name) == false)
            {
                if (string.IsNullOrEmpty(value))
                {
                    this.options.ConnectHeaders.Remove(name);
                }
                else
                {
                    this.options.ConnectHeaders[name] = value;
                }
            }
        }

        /// <summary>
        /// ConnectionToCYarpServer，Create用于接受CYarpServerTransportConnectionListener
        /// </summary> 
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <returns></returns>
        public async Task<ICYarpListener> ListenAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(this.disposeTokenSource.IsCancellationRequested, this);

            var connection = await this.connectionFactory.CreateServerConnectionAsync(cancellationToken);
            return new CYarpListener(this.connectionFactory, connection, this.logger);
        }

        /// <summary>
        /// ConnectionToCYarpServer，并WillCYarpServerTransport绑定ToTargetServer
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="CYarpConnectException"></exception>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        public async Task TransportAsync(CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(this.disposeTokenSource.IsCancellationRequested, this);

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.disposeTokenSource.Token);
            await this.TransportCoreAsync(linkedTokenSource.Token);
        }

        /// <summary>
        /// ConnectionToCYarpServer，并WillCYarpServerTransport绑定ToTargetServer
        /// </summary> 
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="CYarpConnectException"></exception> 
        /// <exception cref="OperationCanceledException"></exception>
        private async Task TransportCoreAsync(CancellationToken cancellationToken)
        {
            await using var connection = await this.connectionFactory.CreateServerConnectionAsync(cancellationToken);
            if (this.connectionFactory.ServerHttp2Supported)
            {
                Log.LogHttp2Connected(this.logger, this.options.ServerUri);
            }
            else
            {
                Log.LogHttp11Connected(this.logger, this.options.ServerUri);
            }

            Guid? tunnelId;
            while ((tunnelId = await connection.ReadTunnelIdAsync(cancellationToken)) != null)
            {
                _ = this.BindTunnelIOAsync(tunnelId.Value, [connection.Closed, cancellationToken]);
            }

            Log.LogDisconnected(this.logger, this.options.ServerUri);
        }


        /// <summary>
        /// 绑定tunnelIO
        /// </summary> 
        /// <param name="tunnelId"></param>
        /// <param name="cancellationTokens"></param>
        private async Task BindTunnelIOAsync(Guid tunnelId, CancellationToken[] cancellationTokens)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationTokens);
            await this.BindTunnelIOCoreAsync(tunnelId, linkedTokenSource.Token);
        }

        /// <summary>
        /// 绑定tunnelIO
        /// </summary> 
        /// <param name="tunnelId"></param>
        /// <param name="cancellationToken"></param>
        private async Task BindTunnelIOCoreAsync(Guid tunnelId, CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                Log.LogTunnelCreating(this.logger, tunnelId, this.options.TargetUri);
                await using var targetTunnel = await this.connectionFactory.CreateTargetTunnelAsync(cancellationToken);

                Log.LogTunnelCreating(this.logger, tunnelId, this.options.ServerUri);
                await using var serverTunnel = await this.connectionFactory.CreateServerTunnelAsync(tunnelId, cancellationToken);

                var count = Interlocked.Increment(ref this.tunnelCount);
                Log.LogTunnelCreated(this.logger, tunnelId, stopwatch.Elapsed, count);

                var server2Target = serverTunnel.CopyToAsync(targetTunnel, cancellationToken);
                var target2Server = targetTunnel.CopyToAsync(serverTunnel, cancellationToken);
                var task = await Task.WhenAny(server2Target, target2Server);

                stopwatch.Stop();
                count = Interlocked.Decrement(ref this.tunnelCount);

                if (task == server2Target)
                {
                    Log.LogTunnelClosed(this.logger, tunnelId, this.options.ServerUri, stopwatch.Elapsed, count);
                }
                else
                {
                    Log.LogTunnelClosed(this.logger, tunnelId, this.options.TargetUri, stopwatch.Elapsed, count);
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
        /// When tunnel exception occurs
        /// </summary>
        /// <param name="ex">Exception</param>
        protected virtual void OnTunnelException(Exception ex)
        {
            this.options.TunnelErrorCallback?.Invoke(ex);
        }


        /// <summary>
        /// Dispose resources
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
        /// Dispose resources
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            this.connectionFactory.Dispose();
            this.disposeTokenSource.Dispose();
        }


        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "ConnectionToServer{address}Success")]
            public static partial void LogHttp11Connected(ILogger logger, Uri address);

            [LoggerMessage(LogLevel.Information, "ConnectionToServer{address}Success，Already启用h2多路复用")]
            public static partial void LogHttp2Connected(ILogger logger, Uri address);

            [LoggerMessage(LogLevel.Warning, "AlreadyDisconnectAndServer{address}Connection")]
            public static partial void LogDisconnected(ILogger logger, Uri address);

            [LoggerMessage(LogLevel.Information, "[{tunnelId}] Creating tunnel to {address}..")]
            public static partial void LogTunnelCreating(ILogger logger, Guid tunnelId, Uri address);

            [LoggerMessage(LogLevel.Information, "[{tunnelId}] Tunnel creation completed in {elapsed}, current tunnel count: {tunnelCount}")]
            public static partial void LogTunnelCreated(ILogger logger, Guid tunnelId, TimeSpan elapsed, int tunnelCount);

            [LoggerMessage(LogLevel.Information, "[{tunnelId}] Tunnel closed by {address}, lifetime: {lifeTime}, current tunnel count: {tunnelCount}")]
            public static partial void LogTunnelClosed(ILogger logger, Guid tunnelId, Uri address, TimeSpan lifeTime, int tunnelCount);

            [LoggerMessage(LogLevel.Warning, "[{tunnelId}] Tunnel encountered exception: {reason}")]
            public static partial void LogTunnelError(ILogger logger, Guid tunnelId, string? reason);
        }
    }
}
