using System;

namespace CYarp.Server
{
    /// <summary>
    /// IClient的授权验证Builder
    /// </summary>
    public interface IClientAuthorizationBuilder
    {
        /// <summary>
        /// 应用程序的服务
        /// </summary>
        IServiceProvider ApplicationServices { get; set; }
    }
}
