
using CYarp.Server;
using Microsoft.AspNetCore.Builder;

namespace Cyarp.Sample.PublicReverseProxy
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddCYarp().Configure(builder.Configuration.GetSection(nameof(CYarpOptions)));
            builder.Host.ConfigureHostOptions(host =>
            {
                host.ShutdownTimeout = TimeSpan.FromSeconds(1d);
            });

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();


            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            //app.UseAuthorization();

            app.UseCYarp();
            app.MapCYarp<DomainClientIdProvider>();
            //app.Map("/{**any}", async (HttpContext context, IClientViewer clientViewer) =>
            //{
            //    var domain = context.Request.Host.Host;
            //    if (clientViewer.TryGetValue(domain, out var client))
            //    {
            //        await client.ForwardHttpAsync(context);
            //    }
            //    else
            //    {
            //        context.Response.StatusCode = StatusCodes.Status502BadGateway;
            //    }
            //});


            app.MapControllers();

            app.Run();
        }
    }
}
