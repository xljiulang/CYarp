using CYarp.Server;

namespace Cyarp.Sample.PublicReverseProxy;


public class DomainClientIdProvider : IClientIdProvider
{
    /// <summary>
    /// 使用请求头的HOST值做IClient的Id
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public ValueTask<string?> GetClientIdAsync(HttpContext context)
    {
        var domain = context.Request.Headers["HOST"].ToString().ToLower();
        return ValueTask.FromResult<string?>(domain);
    }
}


