using Microsoft.Extensions.Logging;

namespace CYarp.Server.Clients
{
    static partial class ClientLog
    {
        [LoggerMessage(LogLevel.Warning, "Connection {connectionId} request invalid: {message}")]
        public static partial void LogInvalidRequest(ILogger<Client> logger, string connectionId, string message);

        [LoggerMessage(LogLevel.Information, "[{clientId}] {protocol} long connection established, current total clients: {count}")]
        public static partial void LogConnected(ILogger<Client> logger, string clientId, TransportProtocol protocol, int count);

        [LoggerMessage(LogLevel.Warning, "[{clientId}] {protocol} long connection disconnected ({reason}), current total clients: {count}")]
        public static partial void LogDisconnected(ILogger<Client> logger, string clientId, TransportProtocol protocol, string reason, int count);


        [LoggerMessage(LogLevel.Debug, "[{clientId}] Sent PING request")]
        public static partial void LogSendPing(ILogger<Client> logger, string clientId);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] Received PING request")]
        public static partial void LogRecvPing(ILogger<Client> logger, string clientId);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] Received PONG response")]
        public static partial void LogRecvPong(ILogger<Client> logger, string clientId);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] Received unknown data: {text}")]
        public static partial void LogRecvUnknown(ILogger<Client> logger, string clientId, string text);

        [LoggerMessage(LogLevel.Debug, "[{clientId}] Connection closed")]
        public static partial void LogClosed(ILogger<Client> logger, string clientId);
    }
}
