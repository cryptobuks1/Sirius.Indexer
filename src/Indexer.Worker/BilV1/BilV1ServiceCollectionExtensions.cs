using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Worker.BilV1
{
    public static class BilV1ServiceCollectionExtensions
    {
        public static IServiceCollection AddBilV1(this IServiceCollection services)
        {
            services.AddSingleton<BilV1ApiClientProvider>();
            services.AddTransient<BillV1TransfersMonitor>();

            return services;
        }
    }
}
