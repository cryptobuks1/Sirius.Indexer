using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sdk.Server.Loggin;

namespace Indexer.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.Title = "Sirius Indexer Worker";

            using (var loggerFactory = LogConfigurator.Configure("Sirius", ApplicationEnvironment.Config["SeqUrl"]))
            {
                var logger = loggerFactory.CreateLogger<Program>();

                try
                {
                    logger.LogInformation("Application is being started");

                    CreateHostBuilder(loggerFactory).Build().Run();

                    logger.LogInformation("Application has been stopped");
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Application has been terminated unexpectedly");
                }
            }
        }

        private static IHostBuilder CreateHostBuilder(ILoggerFactory loggerFactory) =>
            new HostBuilder()
                .SwisschainService<Startup>(options =>
                {
                    options.UseLoggerFactory(loggerFactory);
                    options.WithWebJsonConfigurationSource(ApplicationEnvironment.Config["RemoteSettingsUrl"]);
                });
    }
}
