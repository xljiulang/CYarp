using CYarp.Server;
using CYarpServer.StateStrorages;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using System;
using System.IO;
using System.Security.Claims;

namespace CYarpServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 注册CYarp服务端组件
            builder.Services.AddCYarp()
                .Configure(builder.Configuration.GetSection(nameof(CYarpOptions)))
                .AddRedisClientStateStorage(builder.Configuration.GetSection(nameof(RedisClientStateStorageOptions)));

            // asp.net的jwt认证等 
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            builder.Services.Configure<JwtTokenOptions>(builder.Configuration.GetSection(nameof(JwtTokenOptions)));
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<JwtTokenOptions>>((jwt, jwtTokenOptions) =>
            {
                jwt.TokenValidationParameters = jwtTokenOptions.Value.GetParameters();
            });

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

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapCYarp(ctx => ctx.User.FindFirstValue(ClaimTypes.Sid)).RequireAuthorization(o => o.RequireRole("Client"));
            app.Map("/{**catchall}", HttpForwardHandler.HandleAsync).RequireAuthorization(p => p.RequireRole("Mobile"));

            app.Run();
        }
    }
}
