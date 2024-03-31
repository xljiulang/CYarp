using CYarp.Client;
using CYarp.Server;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace CYarpBench
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddSingleton<HttpForwardMiddleware>();
            builder.Services.AddHostedService<Http11Client>();
            builder.Services.AddHostedService<Http2Client>();
            builder.Services.AddHostedService<WebSocketWithHttp11Client>();
            builder.Services.AddHostedService<WebSocketWithHttp2Client>();

            builder.Services.AddCYarp()
                .Configure(builder.Configuration.GetSection(nameof(CYarpOptions)))
                .AddClientIdProvider<DomainClientIdProvider>();

            string[] names = ["Http11Client", "Http2Client", "WebSocketWithHttp11Client", "WebSocketWithHttp2Client"];
            foreach (var name in names)
            {
                builder.Services.Configure<CYarpClientOptions>(name, builder.Configuration.GetSection(nameof(CYarpClientOptions) + ":" + name));
            }

            builder.Host.ConfigureHostOptions(host =>
            {
                host.ShutdownTimeout = TimeSpan.FromSeconds(1d);
            });

            var app = builder.Build();

            app.UseCYarp().AllowAnonymous();
            app.UseMiddleware<HttpForwardMiddleware>();

            app.Run();
        }
    }
}
