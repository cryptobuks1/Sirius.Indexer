using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Common.Persistence
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services, string connectionString)
        {
            services.AddSingleton<IAssetsRepository, AssetsRepository>();

            return services;
        }
    }
}
