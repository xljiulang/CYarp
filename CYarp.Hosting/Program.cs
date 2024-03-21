using CYarp.Client;
using CYarp.Server;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using System.IO;

namespace CYarp.Hosting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 注册CYarp服务端组件
            builder.Services.AddCYarp();
            builder.Services.Configure<CYarpOptions>(builder.Configuration.GetSection(nameof(CYarpOptions)));

            // 注册CYarp客户端组件
            // 这是demo为了方便，把服务端与客户端都集成在一个Hosting里
            builder.Services.AddHostedService<CYarpClientHostedService>();
            builder.Services.Configure<CYarpClientOptions>(builder.Configuration.GetSection(nameof(CYarpClientOptions)));

            // asp.net的jwt认证、控制器等
            builder.Services.AddControllers();
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            builder.Services.Configure<JwtTokenOptions>(builder.Configuration.GetSection(nameof(JwtTokenOptions)));
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<JwtTokenOptions>>((jwt, jwtTokenOptions) =>
            {
                jwt.TokenValidationParameters = jwtTokenOptions.Value.GetParameters();
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

            app.UseAuthentication();
            app.UseCYarp();

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
