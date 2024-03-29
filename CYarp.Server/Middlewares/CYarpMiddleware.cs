using CYarp.Server.Features;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace CYarp.Server.Middlewares
{
    sealed class CYarpMiddleware : IMiddleware
    {
        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            var feature = new CYarpFeature(context);
            context.Features.Set<ICYarpFeature>(feature);
            return next(context);
        }
    }
}