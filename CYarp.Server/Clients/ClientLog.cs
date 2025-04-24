using Microsoft.Extensions.Logging;

namespace CYarp.Server.Clients
{
    static partial class ClientLog
    {
        [LoggerMessage(LogLevel.Warning, "连接{connectionId}请求无效：{message}")]
        public static partial void LogInvalidRequest(ILogger<Client> logger, string connectionId, string message);

        [LoggerMessage(LogLevel.Information, "[{clientId}] {protocol}长连接成功，系统当前客户端总数为{count}")]
        public static partial void LogConnected(ILogger<Client> logger, string clientId, TransportProtocol protocol, int count);

        [LoggerMessage(LogLevel.Warning, "[{clientId}] {protocol}长连接断开，系统当前客户端总数为{count}")]
        public static partial void LogDisconnected(ILogger<Client> logger, string clientId, TransportProtocol protocol, int count);


        [LoggerMessage(LogLevel.Debug, "[{clientId}] 发出PING请求")]
        public static partial void LogSendPing(ILogger<Client> logger, string clientId);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] 收到PING请求")]
        public static partial void LogRecvPing(ILogger<Client> logger, string clientId);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] 收到PONG回应")]
        public static partial void LogRecvPong(ILogger<Client> logger, string clientId);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] 收到未知数据: {text}")]
        public static partial void LogRecvUnknown(ILogger<Client> logger, string clientId, string text);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] 连接已关闭")]
        public static partial void LogClosed(ILogger<Client> logger, string clientId);
    }
}
