using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于和frp bench测试的身份验证中间件
    /// </summary>
    sealed class AuthenticationMiddleware : IMiddleware
    {
        private static readonly string scheme = "CustomDomain";

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (AuthenticationHeaderValue.TryParse(context.Request.Headers.Authorization, out var authorization))
            {
                var domain = authorization.Parameter;
                if (authorization.Scheme == scheme && domain != null)
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity([new(ClaimTypes.Sid, domain)], scheme));
                }
            }

            return next(context);
        }
    }
}
