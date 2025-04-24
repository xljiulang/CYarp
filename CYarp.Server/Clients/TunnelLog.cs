using Microsoft.Extensions.Logging;
using System;

namespace CYarp.Server.Clients
{
    static partial class TunnelLog
    {
        [LoggerMessage(LogLevel.Information, "[{clientId}] 请求创建Tunnel {tunnelId}")]
        public static partial void LogTunnelCreating(ILogger logger, string clientId, TunnelId tunnelId);

        [LoggerMessage(LogLevel.Warning, "[{clientId}] 创建Tunnel {tunnelId} 失败：{reason}")]
        public static partial void LogTunnelCreateFailure(ILogger logger, string clientId, TunnelId tunnelId, string? reason);

        [LoggerMessage(LogLevel.Information, "[{clientId}] 创建了{protocol}协议Tunnel {tunnelId}，过程耗时{elapsed}，其当前{tunnelType}总数为{tunnelCount}")]
        public static partial void LogTunnelCreate(ILogger logger, string clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan elapsed, string tunnelType, int tunnelCount);

        [LoggerMessage(LogLevel.Information, "[{clientId}] 关闭了{protocol}协议Tunnel {tunnelId}，生命周期为{lifeTime}，其当前{tunnelType}总数为{tunnelCount}")]
        public static partial void LogTunnelClosed(ILogger logger, string? clientId, TransportProtocol protocol, TunnelId tunnelId, TimeSpan lifeTime, string tunnelType, int tunnelCount);
    }
}
