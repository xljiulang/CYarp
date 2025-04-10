using CYarp.Client;
using CYarp.Client.AspNetCore;
using Microsoft.Extensions.Options;

namespace CYarpClientAspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Ìí¼ÓcyarpµÄ¼àÌýÆ÷
            builder.Services.AddCYarpListener();
            builder.Services.Configure<CYarpClientOptions>(builder.Configuration.GetSection(nameof(CYarpClientOptions)));
            builder.WebHost.ConfigureKestrel(kestrel =>
            {
                kestrel.ListenLocalhost(5000);
                kestrel.Listen(new CYarpEndPoint(kestrel.ApplicationServices.GetRequiredService<IOptions<CYarpClientOptions>>().Value));
            });

            var app = builder.Build();

            app.UseStaticFiles();
            app.Run();
        }
    }
}
