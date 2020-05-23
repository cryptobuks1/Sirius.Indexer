using System;
using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Common.Telemetry
{
    public static class AppInsightServiceCollectionExtensions
    {
        public static IServiceCollection AddAppInsight(this IServiceCollection services, Action<AppInsightOptions> optionsBuilder)
        {
            var options = new AppInsightOptions();

            optionsBuilder.Invoke(options);

            services.AddSingleton<IAppInsight>(x => new AppInsight(options));

            return services;
        }
    }
}
