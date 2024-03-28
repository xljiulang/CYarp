using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CYarpBench
{
    /// <summary>
    /// 适用于和frp bench测试的身份验证中间件
    /// </summary>
    sealed class AuthenticationMiddleware : IMiddleware
    {
        private static readonly ClaimsPrincipal emptyUser = new(new ClaimsIdentity([], "bench"));

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            context.User = emptyUser;
            return next(context);
        }
    }
}
