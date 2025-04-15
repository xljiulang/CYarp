using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarp.Server
{
    /// <summary>
    /// IClient的Id的提供者
    /// </summary>
    public interface IClientIdProvider
    {
        /// <summary>
        /// 尝试获取IClient的Id
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        ValueTask<string?> GetClientIdAsync(HttpContext context);
    }
}
