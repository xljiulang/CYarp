using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CYarp.Server
{
    /// <summary>
    /// CYarp的IClient的查看器
    /// </summary>
    public interface IClientViewer : IReadOnlyCollection<IClient>
    {
        /// <summary>
        /// 使用clientId尝试获取客户端实例
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="client">客户端实例</param>
        /// <returns></returns>
        bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IClient client);
    }
}
