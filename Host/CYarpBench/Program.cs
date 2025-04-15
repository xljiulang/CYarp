using CYarp.Client;
using CYarp.Server;
using CYarpBench.Clients;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace CYarpBench
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddHostedService<DefaultClient>();
            builder.Services.AddHostedService<Http11Client>();
            builder.Services.AddHostedService<Http2Client>();
            builder.Services.AddHostedService<WebSocketWithHttp11Client>();
            builder.Services.AddHostedService<WebSocketWithHttp2Client>();

            builder.Services.AddCYarp()
                .Configure(builder.Configuration.GetSection(nameof(CYarpOptions)));

            var clientNames = typeof(ClientBase).Assembly.GetTypes()
                .Where(item => item.IsAbstract == false && typeof(ClientBase).IsAssignableFrom(item))
                .Select(item => item.Name);
            foreach (var name in clientNames)
            {
                var key = $"{nameof(CYarpClientOptions)}:{name}";
                builder.Services.Configure<CYarpClientOptions>(name, builder.Configuration.GetSection(key));
            }

            builder.Host.ConfigureHostOptions(host =>
            {
                host.ShutdownTimeout = TimeSpan.FromSeconds(1d);
            });

            // serilog
            builder.Host.UseSerilog((context, logger) =>
            {
                var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                logger.ReadFrom.Configuration(context.Configuration)
                    .Enrich.FromLogContext()
                    .WriteTo.Console(outputTemplate: template)
                    .WriteTo.File(Path.Combine("logs", @"log.txt"), rollingInterval: RollingInterval.Day, outputTemplate: template);
            }, writeToProviders: false);

            var app = builder.Build();

            app.UseCYarp();
            app.MapCYarp<DomainClientIdProvider>();
            app.Map("/{**any}", HttpForwardHandler.HandlerAsync);

            app.Run();
        }
    }
}
