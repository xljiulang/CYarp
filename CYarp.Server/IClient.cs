using Microsoft.AspNetCore.Http;
using System;
using System.Net;
using System.Security.Claims;
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
        /// 获取唯一标识
        /// </summary>
        string Id { get; }

        /// <summary>
        /// 获取客户端连接的目标http服务器地址
        /// </summary>
        Uri Destination { get; }

        /// <summary>
        /// 获取关联的用户信息
        /// </summary>
        ClaimsPrincipal User { get; }

        /// <summary>
        /// 获取连接协议
        /// </summary>
        string Protocol { get; }

        /// <summary>
        /// 获取远程终结点
        /// </summary>
        IPEndPoint? RemoteEndpoint { get; }

        /// <summary>
        /// 获取创建时间
        /// </summary>
        DateTimeOffset CreationTime { get; }

        /// <summary>
        /// 转发http请求
        /// </summary>
        /// <param name="httpContext"></param>
        /// <param name="requestConfig"></param>
        /// <param name="transformer"></param>
        /// <returns></returns>
        ValueTask<ForwarderError> ForwardHttpAsync(HttpContext httpContext, ForwarderRequestConfig? requestConfig = default, HttpTransformer? transformer = default);
    }
}
