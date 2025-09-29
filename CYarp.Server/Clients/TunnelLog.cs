using Microsoft.Extensions.Logging;
using System;

namespace CYarp.Server.Clients
{
    static partial class TunnelLog
    {
        [LoggerMessage(LogLevel.Warning, "Connection {connectionId} request invalid: {message}")]
        public static partial void LogInvalidRequest(ILogger logger, string connectionId, string message);

        [LoggerMessage(LogLevel.Warning, "Connection {connectionId} passed invalid tunnelId: {tunnelId}")]
        public static partial void LogInvalidTunnelId(ILogger logger, string connectionId, TunnelId tunnelId);


        [LoggerMessage(LogLevel.Information, "[{clientId}] Requested to create Tunnel {tunnelId}")]
        public static partial void LogTunnelCreating(ILogger<Tunnel> logger, string clientId, TunnelId tunnelId);

        [LoggerMessage(LogLevel.Warning, "[{clientId}] Failed to create Tunnel {tunnelId}: {reason}")]
        public static partial void LogTunnelCreateFailure(ILogger<Tunnel> logger, string clientId, TunnelId tunnelId, string? reason);

        [LoggerMessage(LogLevel.Information, "[{clientId}] Created {protocol} protocol Tunnel {tunnelId}, took {elapsed}, current {tunnelType} total: {tunnelCount}")]
        public static partial void LogTunnelCreate(ILogger<Tunnel> logger, string clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan elapsed, TunnelType tunnelType, int tunnelCount);

        [LoggerMessage(LogLevel.Information, "[{clientId}] Closed {protocol} protocol Tunnel {tunnelId}, lifetime: {lifeTime}, current {tunnelType} total: {tunnelCount}")]
        public static partial void LogTunnelClosed(ILogger<Tunnel> logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan lifeTime, TunnelType tunnelType, int tunnelCount);
    }
}
