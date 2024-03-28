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
            builder.Services.AddHostedService<CYarpClientHostedService>();

            builder.Services.AddCYarp()
                .Configure(builder.Configuration.GetSection(nameof(CYarpOptions)))
                .AddClientIdProvider<DomainClientIdProvider>();

            builder.Services.Configure<CYarpClientOptions>(builder.Configuration.GetSection(nameof(CYarpClientOptions)));

            builder.Host.ConfigureHostOptions(host =>
            {
                host.ShutdownTimeout = TimeSpan.FromSeconds(1d);
            });

            var app = builder.Build();

            app.UseCYarpAnonymous();
            app.UseMiddleware<HttpForwardMiddleware>();

            app.Run();
        }
    }
}
