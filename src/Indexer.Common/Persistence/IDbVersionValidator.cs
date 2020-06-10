using System.Threading.Tasks;

namespace Indexer.Common.Persistence
{
    public interface IDbVersionValidator
    {
        Task Validate();
    }
}
