
using CYarp.Client.AspNetCore;
using Microsoft.Extensions.Options;

namespace Cyarp.Sample.IntranetSite1
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

            // Add SSE endpoint for testing request cancellation
            app.MapGet("/sse", async (HttpContext httpContext) =>
            {
                httpContext.Response.Headers.Append("Content-Type", "text/event-stream");
                httpContext.Response.Headers.Append("Cache-Control", "no-cache");
                httpContext.Response.Headers.Append("Connection", "keep-alive");
                
                var counter = 0;
                while (!httpContext.RequestAborted.IsCancellationRequested)
                {
                    counter++;
                    var message = $"data: Counter: {counter} at {DateTime.Now:HH:mm:ss.fff}\n\n";
                    await httpContext.Response.WriteAsync(message);
                    await httpContext.Response.Body.FlushAsync();
                    await Task.Delay(1000, httpContext.RequestAborted);
                }
            });

            app.MapControllers();

            app.Run();
        }
    }
}
