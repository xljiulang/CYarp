using CYarp.Client;
using CYarpServer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;

namespace CYarpClient
{
    class Program
    {
        static void Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.Configure<CYarpClientOptions>(context.Configuration.GetSection(nameof(CYarpClientOptions)));
                    services.AddHostedService<ClientHostedService>();
                })
                // serilog
                .UseSerilog((context, logger) =>
                {
                    var template = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}]{NewLine}{SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}";
                    logger.ReadFrom.Configuration(context.Configuration)
                        .Enrich.FromLogContext()
                        .WriteTo.Console(outputTemplate: template)
                        .WriteTo.File(Path.Combine("logs", @"log.txt"), rollingInterval: RollingInterval.Day, outputTemplate: template);
                }, writeToProviders: false)
                .Build();

            host.Run();

        }
    }
}
