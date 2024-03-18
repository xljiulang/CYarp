using System;
using System.Net.Sockets;

namespace CYarp.Server.Configs
{
    /// <summary>
    /// TcpKeepAlive配置
    /// </summary>
    public record TcpKeepAliveConfig
    {
        /// <summary>
        /// TcpKeepAliveTime
        /// 默认30s
        /// </summary>
        public TimeSpan Time { get; init; } = TimeSpan.FromSeconds(30d);

        /// <summary>
        /// TcpKeepAliveInterval
        /// 默认10s
        /// </summary>
        public TimeSpan Interval { get; init; } = TimeSpan.FromSeconds(10d);

        /// <summary>
        /// TcpKeepAliveRetryCount
        /// 默认3
        /// </summary>
        public int RetryCount { get; init; } = 3;


        /// <summary>
        /// 设置Socket的心跳检测
        /// </summary>
        /// <param name="socket">socket</param> 
        /// <returns></returns>
        public bool SetTcpKeepAlive(Socket socket)
        {
            try
            {
                var time = (int)this.Time.TotalSeconds;
                var interval = (int)this.Interval.TotalSeconds;
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveTime, time);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveInterval, interval);
                socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.TcpKeepAliveRetryCount, this.RetryCount);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
