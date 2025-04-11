using Microsoft.AspNetCore.Connections;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CYarp.Client.AspNetCore
{
    sealed partial class CYarpConnectionListener : IConnectionListener
    {
        private readonly CYarpEndPoint endPoint;
        private readonly ILogger logger;
        private readonly CYarpClient client;
        private readonly CancellationTokenSource closedTokenSource = new();
        private IAsyncEnumerator<ConnectionContext>? connnections;

        public EndPoint EndPoint => this.endPoint;

        public CYarpConnectionListener(CYarpEndPoint endPoint, ILogger logger)
        {
            this.endPoint = endPoint;
            this.logger = logger;
            this.client = new CYarpClient(endPoint.Options, logger);
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
                if (this.connnections == null)
                {
                    this.connnections = this.AcceptAllAsync(cancellationToken).GetAsyncEnumerator(cancellationToken);
                }

                try
                {
                    if (await this.connnections.MoveNextAsync())
                    {
                        return this.connnections.Current;
                    }
                    else
                    {
                        await this.connnections.DisposeAsync();
                        this.connnections = null;

                        Log.LogConnectError(this.logger, this.endPoint, "连接被中断");
                        await Task.Delay(this.endPoint.ReconnectInterval, cancellationToken);
                    }
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    Log.LogConnectError(this.logger, this.endPoint, "操作被用户取消");
                    return null;
                }
                catch (CYarpConnectException ex) when (ex.ErrorCode == CYarpConnectError.Unauthorized)
                {
                    Log.LogConnectError(this.logger, this.endPoint, "连接的身份认证不通过");
                    throw;
                }
                catch (Exception ex)
                {
                    if (this.connnections != null)
                    {
                        await this.connnections.DisposeAsync();
                    }
                    this.connnections = null;

                    Log.LogConnectError(this.logger, this.endPoint, ex.Message);
                    await Task.Delay(this.endPoint.ReconnectInterval, cancellationToken);
                }
            }
        }

        private async IAsyncEnumerable<ConnectionContext> AcceptAllAsync([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            await using var listener = await this.client.ListenAsync(cancellationToken);
            Log.LogConnected(this.logger, this.endPoint);

            await foreach (var stream in listener.AcceptAllAsync(cancellationToken))
            {
                yield return new CYarpConnectionContext(stream, this.EndPoint);
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

            if (this.connnections != null)
            {
                await this.connnections.DisposeAsync();
            }

            this.client.Dispose();
        }


        static partial class Log
        {
            [LoggerMessage(LogLevel.Information, "连接到服务器{endPoint}成功")]
            public static partial void LogConnected(ILogger logger, EndPoint endPoint);

            [LoggerMessage(LogLevel.Warning, "已断开与服务器{endPoint}的连接")]
            public static partial void LogDisconnected(ILogger logger, EndPoint endPoint);

            [LoggerMessage(LogLevel.Warning, "连接到服务器{endPoint}异常：{reason}")]
            public static partial void LogConnectError(ILogger logger, EndPoint endPoint, string? reason);
        }
    }
}
