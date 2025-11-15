using Microsoft.Extensions.Logging;
using System;

namespace CYarp.Server.Clients
{
    static partial class TunnelLog
    {
        [LoggerMessage(LogLevel.Warning, "Invalid request from connection {connectionId}: {message}")]
        public static partial void LogInvalidRequest(ILogger logger, string connectionId, string message);

        [LoggerMessage(LogLevel.Warning, "Connection {connectionId} passed invalid tunnelId: {tunnelId}")]
        public static partial void LogInvalidTunnelId(ILogger logger, string connectionId, TunnelId tunnelId);


        [LoggerMessage(LogLevel.Information, "[{clientId}] Request creating Tunnel {tunnelId}")]
        public static partial void LogTunnelCreating(ILogger<Tunnel> logger, string clientId, TunnelId tunnelId);

        [LoggerMessage(LogLevel.Warning, "[{clientId}] Create Tunnel {tunnelId} failed: {reason}")]
        public static partial void LogTunnelCreateFailure(ILogger<Tunnel> logger, string clientId, TunnelId tunnelId, string? reason);

        [LoggerMessage(LogLevel.Information, "[{clientId}] Created {protocol} protocol Tunnel {tunnelId}, elapsed {elapsed}, current {tunnelType} count {tunnelCount}")]
        public static partial void LogTunnelCreate(ILogger<Tunnel> logger, string clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan elapsed, TunnelType tunnelType, int tunnelCount);

        [LoggerMessage(LogLevel.Information, "[{clientId}] Closed {protocol} protocol Tunnel {tunnelId}, lifetime {lifeTime}, current {tunnelType} count {tunnelCount}")]
        public static partial void LogTunnelClosed(ILogger<Tunnel> logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan lifeTime, TunnelType tunnelType, int tunnelCount);
    }
}
