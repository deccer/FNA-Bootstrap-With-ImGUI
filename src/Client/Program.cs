using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Client
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            DllMap.Initialise();
            var serviceProvider = CreateServiceProvider();

            using var mainGame = serviceProvider.GetService<MainGame>();
            mainGame!.Run();
        }

        private static IServiceProvider CreateServiceProvider()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .WriteTo.File("client.log")
                .CreateLogger();

            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", false, true)
                .Build();

            var services = new ServiceCollection();
            services.AddSingleton(Log.Logger);
            services.AddSingleton<IConfiguration>(configuration);
            services.AddSingleton<MainGame>();
            return services.BuildServiceProvider();
        }
    }
}
