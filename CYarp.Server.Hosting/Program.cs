using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CYarp.Server.Hosting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCYarp();
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();

            var app = builder.Build();

            app.UseAuthentication();
            app.UseCYarpClient();

            app.UseAuthorization();

            app.Map("/{**any}", CYarpHandler.InvokeAsync)
                //.RequireAuthorization()
                ;
            app.Run();
        }
    }
}
