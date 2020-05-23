using Microsoft.Extensions.DependencyInjection;

namespace Indexer.Worker.Jobs
{
    internal static class JobsServiceCollectionExtensions
    {
        public static IServiceCollection AddJobs(this IServiceCollection services)
        {
            services.AddSingleton<SecondPassIndexingJobsManager>();
            services.AddSingleton<OngoingIndexingJobsManager>();

            return services;
        }
    }
}
