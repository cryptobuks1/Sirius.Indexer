using Ductus.FluentDocker.Builders;

namespace IndexerTests.Sdk.Containers
{
    public static class ContainerRemover
    {
        public static void RemoveIfExists(string containerName, string imageName)
        {
            new Builder()
                .UseContainer()
                .WithName(containerName)
                .UseImage(imageName)
                .ReuseIfExists()
                .Build()
                .Remove(force: true);
        }
    }
}
