using CYarp.Client.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CYarpClientAspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<CYarpEndPoint>(builder.Configuration.GetSection(nameof(CYarpEndPoint)));

            // Register cyarp listener
            builder.Services.AddCYarpListener();

            builder.WebHost.ConfigureKestrel(kestrel =>
            {
                kestrel.ListenLocalhost(5000);

                // Configure a cyarp endpoint
                var endPoint = kestrel.ApplicationServices.GetRequiredService<IOptions<CYarpEndPoint>>().Value;
                kestrel.ListenCYarp(endPoint);
            });

            var app = builder.Build();
            app.UseStaticFiles();
            app.UseRouting();
            app.MapGet("/", context =>
            {
                return context.Response.WriteAsync("ok");
            });
            app.Run();
        }
    }
}
