using CYarp.Server;

namespace Cyarp.Sample.PublicReverseProxy;


public class DomainClientIdProvider : IClientIdProvider
{
    /// <summary>
    /// Use HOST header value as IClientId
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public ValueTask<string?> GetClientIdAsync(HttpContext context)
    {
        var domain = context.Request.Headers["HOST"].ToString().ToLower();
        return ValueTask.FromResult<string?>(domain);
    }
}


