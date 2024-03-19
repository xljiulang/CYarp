using Microsoft.AspNetCore.Http;
using System;
using System.Threading;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Forwarder;

namespace CYarp.Server
{
    /// <summary>
    /// 客户端接口
    /// </summary>
    public interface IClient : IDisposable
    {
        /// <summary>
        /// 获取客户端唯一标识
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 获取客户端连接的目标http服务器地址
        /// </summary>
        Uri Destination { get; }

        /// <summary>
        /// 请求创建tunnel
        /// </summary>
        /// <param name="tunnelId">随机的tunnel标识</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task CreateTunnelAsync(Guid tunnelId, CancellationToken cancellationToken = default);

        /// <summary>
        /// 转发http请求
        /// </summary>
        /// <param name="context"></param>
        /// <param name="requestConfig"></param>
        /// <param name="transformer"></param>
        /// <returns></returns>
        ValueTask<ForwarderError> ForwardHttpAsync(HttpContext context, ForwarderRequestConfig? requestConfig = default, HttpTransformer? transformer = default);

        /// <summary>
        /// 等待直到关闭
        /// </summary>
        /// <returns></returns>
        Task WaitForCloseAsync();
    }
}
