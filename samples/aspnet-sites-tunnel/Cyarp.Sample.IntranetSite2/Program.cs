
using CYarp.Client.AspNetCore;
using Microsoft.Extensions.Options;

namespace Cyarp.Sample.IntranetSite2
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<CYarpEndPoint>(builder.Configuration.GetSection(nameof(CYarpEndPoint)));
            builder.Services.AddCYarpListener();
            builder.WebHost.ConfigureKestrel(kestrel =>
            { 
                var endPoint = kestrel.ApplicationServices.GetRequiredService<IOptions<CYarpEndPoint>>().Value;
                kestrel.ListenCYarp(endPoint);
            });

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}
