using System;
using System.Net;

namespace CYarp.Client.AspNetCore
{
    /// <summary>
    /// Cyarp终结点
    /// </summary>
    public sealed class CYarpEndPoint : EndPoint
    {
        /// <summary>
        /// 获取选项
        /// </summary>
        public CYarpClientOptions Options { get; }

        /// <summary>
        /// 获取或设置断线重连间隔间隔
        /// 默认5s
        /// </summary>
        public TimeSpan ReconnectInterval { get; set; } = TimeSpan.FromSeconds(5d);

        /// <summary>
        /// Cyarp终结点
        /// </summary>
        /// <param name="options">连接选项</param> 
        public CYarpEndPoint(CYarpClientOptions options)
        {
            this.Options = options;
        }

        /// <summary>
        /// 转换为文本
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.Options.ServerUri.ToString();
        }
    }
}
