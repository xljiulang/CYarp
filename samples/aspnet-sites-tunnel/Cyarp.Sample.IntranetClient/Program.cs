using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cyarp.Sample.IntranetClient
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();

            ServiceCollection services = new ServiceCollection();
            services.AddLogging(options =>
            {
                options.AddDebug();
            });
            services.AddSingleton<Form1>();
            var serviceProvider = services.BuildServiceProvider();
            var formMain = serviceProvider.GetRequiredService<Form1>();

            Application.Run(formMain);
        }

    }
}