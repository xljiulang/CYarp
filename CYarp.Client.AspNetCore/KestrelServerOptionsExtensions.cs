using CYarp.Client.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// KestrelServerOptions扩展
    /// </summary>
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// 监听一个CYarp终结点
        /// </summary>
        /// <param name="kestrel"></param>
        /// <param name="endPoint">CYarp终结点</param>
        public static void ListenCYarp(this KestrelServerOptions kestrel, CYarpEndPoint endPoint)
        {
            kestrel.Listen(endPoint);
        }

        /// <summary>
        /// 监听一个CYarp终结点
        /// </summary>
        /// <param name="kestrel"></param>
        /// <param name="endPoint">CYarp终结点</param>
        /// <param name="configure">配置</param>
        public static void ListenCYarp(this KestrelServerOptions kestrel, CYarpEndPoint endPoint, Action<ListenOptions> configure)
        {
            kestrel.Listen(endPoint, configure);
        }
    }
}
