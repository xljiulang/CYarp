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
                        Log.LogConnectError(this.logger, this.endPoint, "连接被断开");
                    }
                }
                catch (CYarpConnectException ex) when (ex.ErrorCode == CYarpConnectError.Unauthorized)
                {
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

        public async ValueTask DisposeAsync()
        {
            if (this.connnections != null)
            {
                await this.connnections.DisposeAsync();
            }

            this.client.Dispose();
        }


        public ValueTask UnbindAsync(CancellationToken cancellationToken = default)
        {
            return ValueTask.CompletedTask;
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
