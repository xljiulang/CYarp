using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore.Connections
{
    sealed partial class CYarpConnectionListener : IConnectionListener
    {
        private readonly CYarpEndPoint endPoint;
        private readonly ILogger logger;
        private readonly CancellationTokenSource closedTokenSource = new();
        private IAsyncEnumerator<ConnectionContext>? connections;

        public EndPoint EndPoint => this.endPoint;

        public CYarpConnectionListener(CYarpEndPoint endPoint, ILogger logger)
        {
            this.endPoint = endPoint;
            this.logger = logger;
        }

        public async ValueTask<ConnectionContext?> AcceptAsync(CancellationToken cancellationToken = default)
        {
            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.closedTokenSource.Token);
            return await this.AcceptCoreAsync(linkedTokenSource.Token);
        }

        private async ValueTask<ConnectionContext?> AcceptCoreAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (this.connections == null)
                {
                    this.connections = this.AcceptAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
                }

                try
                {
                    if (await this.connections.MoveNextAsync())
                    {
                        return this.connections.Current;
                    }
                    else
                    {
                        await this.connections.DisposeAsync();
                        this.connections = null;

                        Log.LogConnectError(this.logger, this.endPoint.ServerUri, "Connection interrupted");
                        await Task.Delay(this.endPoint.ReconnectInterval, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log.LogConnectError(this.logger, this.endPoint.ServerUri, "Operation cancelled by user");
                    return null;
                }
                catch (CYarpConnectException ex) when (ex.ErrorCode >= CYarpConnectError.Unauthorized)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (this.connections != null)
                    {
                        await this.connections.DisposeAsync();
                    }
                    this.connections = null;

                    Log.LogConnectError(this.logger, this.endPoint.ServerUri, ex.Message);
                    await Task.Delay(this.endPoint.ReconnectInterval, cancellationToken);
                }
            }
        }

        private async IAsyncEnumerable<ConnectionContext> AcceptAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var options = new CYarpClientOptions
            {
                ConnectHeaders = this.endPoint.ConnectHeaders,
                ConnectTimeout = this.endPoint.ConnectTimeout,
                KeepAliveInterval = this.endPoint.KeepAliveInterval,
                ServerUri = this.endPoint.ServerUri,
                TargetUri = this.endPoint.TargetUri,
            };

            if (this.endPoint.ConnectHeadersFactory != null)
            {
                options.ConnectHeaders = await this.endPoint.ConnectHeadersFactory.Invoke();
            }

            using var client = new CYarpClient(options, this.logger);
            await using var listener = await client.ListenAsync(cancellationToken);
            Log.LogConnected(this.logger, this.endPoint.ServerUri);

            var localEndPoint = new DnsEndPoint(this.endPoint.TargetUri.Host, this.endPoint.TargetUri.Port);
            var remoteEndPoint = new DnsEndPoint(this.endPoint.ServerUri.Host, this.endPoint.ServerUri.Port);
            await foreach (var stream in listener.AcceptAllAsync(cancellationToken))
            {
                yield return new CYarpConnectionContext(stream)
                {
                    LocalEndPoint = localEndPoint,
                    RemoteEndPoint = remoteEndPoint
                };
            }
        }

        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            this.closedTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }


        public async ValueTask DisposeAsync()
        {
            this.closedTokenSource.Cancel();
            this.closedTokenSource.Dispose();

            if (this.connections != null)
            {
                await this.connections.DisposeAsync();
            }
        }


        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "ConnectionToServer{serverUri}Success")]
            public static partial void LogConnected(ILogger logger, Uri serverUri);

            [LoggerMessage(LogLevel.Warning, "AlreadyDisconnectAndServer{serverUri}Connection")]
            public static partial void LogDisconnected(ILogger logger, Uri serverUri);

            [LoggerMessage(LogLevel.Warning, "ConnectionToServer{serverUri}Exception：{reason}")]
            public static partial void LogConnectError(ILogger logger, Uri serverUri, string? reason);
        }
    }
}
