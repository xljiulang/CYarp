using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace CYarp.Server.Hosting
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddCYarp();
            builder.Services.AddAuthentication();

            var app = builder.Build();
       
            app.UseAuthentication();
            app.UseCYarp();

            app.Run();
        }
    }
}
