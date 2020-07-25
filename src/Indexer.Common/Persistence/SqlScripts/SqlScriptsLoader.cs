using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Indexer.Common.Persistence.SqlScripts
{
    internal sealed class SqlScriptsLoader
    {
        public static async Task<string> Load(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"Indexer.Common.Persistence.SqlScripts.{fileName}";

            await using var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new InvalidOperationException($"Resource {resourceName} is not found");
            }

            using var reader = new StreamReader(stream);

            return reader.ReadToEnd();
        }
    }
}
