using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// 客户端管理器
    /// </summary>
    public interface IClientManager : IReadOnlyCollection<IClient>
    {
        /// <summary>
        /// 添加客户端实例
        /// </summary>
        /// <param name="client">客户端实例</param>
        /// <returns></returns>
        ValueTask<bool> AddAsync(IClient client);

        /// <summary>
        /// 移除客户端实例
        /// </summary>
        /// <param name="client">客户端实例</param>
        /// <returns></returns>
        ValueTask RemoveAsync(IClient client);

        /// <summary>
        /// 使用client尝试获取客户端实例
        /// </summary>
        /// <param name="clientId">客户端id</param>
        /// <param name="client">客户端实例</param>
        /// <returns></returns>
        bool TryGetValue(string clientId, [MaybeNullWhen(false)] out IClient client);
    }
}