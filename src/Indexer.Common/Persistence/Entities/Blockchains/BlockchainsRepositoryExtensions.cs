using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Indexer.Common.ReadModel.Blockchains;

namespace Indexer.Common.Persistence.Entities.Blockchains
{
    public static class BlockchainsRepositoryExtensions
    {
        public static async Task<IReadOnlyCollection<BlockchainMetamodel>> GetAllAsync(this IBlockchainsRepository repository)
        {
            var cursor = default(string);
            var result = new List<BlockchainMetamodel>();

            do
            {
                var page = await repository.GetAllAsync(cursor, 100);

                if (!page.Any())
                {
                    break;
                }

                cursor = page.Last().Id;

                result.AddRange(page);
            } while (true);

            return result;
        }
    }
}
