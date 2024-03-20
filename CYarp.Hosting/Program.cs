using CYarp.Server;
using CYarp.Client;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CYarp.Hosting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCYarp();
            builder.Services.Configure<CYarpOptions>(builder.Configuration.GetSection(nameof(CYarpOptions)));

            builder.Services.AddHostedService<CYarpClientHostedService>();
            builder.Services.Configure<CYarpClientOptions>(builder.Configuration.GetSection(nameof(CYarpClientOptions)));

            builder.Services.AddControllers();
            builder.Services.AddAuthorization();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
            builder.Services.Configure<JwtTokenOptions>(builder.Configuration.GetSection(nameof(JwtTokenOptions)));
            builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme).Configure<IOptions<JwtTokenOptions>>((jwt, jwtTokenOptions) =>
            {
                jwt.TokenValidationParameters = jwtTokenOptions.Value.GetParameters();
            });

            var app = builder.Build();

            app.UseAuthentication();
            app.UseCYarp();

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
