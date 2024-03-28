using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace CYarp.Server
{
    /// <summary>
    /// IClient的Id的提供者
    /// 默认提供者读取CYarpOptions.Authorization.ClientIdClaimType对应的Claim做为Id
    /// </summary>
    public interface IClientIdProvider
    {
        /// <summary>
        /// 获取提供者的名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 尝试获取IClient的Id
        /// </summary>
        /// <param name="context">上下文</param>
        /// <param name="clientId">IClient的Id</param>
        /// <returns></returns>
        bool TryGetClientId(HttpContext context, [MaybeNullWhen(false)] out string clientId);
    }
}
