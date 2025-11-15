using CYarp.Client.AspNetCore;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using System;

namespace Microsoft.AspNetCore.Hosting
{
    /// <summary>
    /// Extensions for KestrelServerOptions
    /// </summary>
    public static class KestrelServerOptionsExtensions
    {
        /// <summary>
        /// Listen on a CYarp endpoint
        /// </summary>
        /// <param name="kestrel"></param>
        /// <param name="endPoint">CYarp endpoint</param>
        public static void ListenCYarp(this KestrelServerOptions kestrel, CYarpEndPoint endPoint)
        {
            kestrel.Listen(endPoint);
        }

        /// <summary>
        /// Listen on a CYarp endpoint
        /// </summary>
        /// <param name="kestrel"></param>
        /// <param name="endPoint">CYarp endpoint</param>
        /// <param name="configure">Configuration</param>
        public static void ListenCYarp(this KestrelServerOptions kestrel, CYarpEndPoint endPoint, Action<ListenOptions> configure)
        {
            kestrel.Listen(endPoint, configure);
        }
    }
}
