using Microsoft.Extensions.Logging;
using System;

namespace CYarp.Server.Clients
{
    static partial class TunnelLog
    {
        [LoggerMessage(LogLevel.Warning, "连接{connectionId}请求无效：{message}")]
        public static partial void LogInvalidRequest(ILogger logger, string connectionId, string message);

        [LoggerMessage(LogLevel.Warning, "连接{connectionId}传递了无效的tunnelId：{tunnelId}")]
        public static partial void LogInvalidTunnelId(ILogger logger, string connectionId, TunnelId tunnelId);


        [LoggerMessage(LogLevel.Information, "[{clientId}] 请求创建Tunnel {tunnelId}")]
        public static partial void LogTunnelCreating(ILogger<Tunnel> logger, string clientId, TunnelId tunnelId);

        [LoggerMessage(LogLevel.Warning, "[{clientId}] 创建Tunnel {tunnelId} 失败：{reason}")]
        public static partial void LogTunnelCreateFailure(ILogger<Tunnel> logger, string clientId, TunnelId tunnelId, string? reason);

        [LoggerMessage(LogLevel.Information, "[{clientId}] 创建了{protocol}协议Tunnel {tunnelId}，过程耗时{elapsed}，其当前{tunnelType}总数为{tunnelCount}")]
        public static partial void LogTunnelCreate(ILogger<Tunnel> logger, string clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan elapsed, TunnelType tunnelType, int tunnelCount);

        [LoggerMessage(LogLevel.Information, "[{clientId}] 关闭了{protocol}协议Tunnel {tunnelId}，生命周期为{lifeTime}，其当前{tunnelType}总数为{tunnelCount}")]
        public static partial void LogTunnelClosed(ILogger<Tunnel> logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan lifeTime, TunnelType tunnelType, int tunnelCount);
    }
}
