using System;

namespace CYarp.Server
{
    /// <summary>
    /// CYarp应用的Builder
    /// </summary>
    public interface ICYarpAppBuilder
    {
        /// <summary>
        /// 应用程序的服务
        /// </summary>
        IServiceProvider ApplicationServices { get; set; }
    }
}
