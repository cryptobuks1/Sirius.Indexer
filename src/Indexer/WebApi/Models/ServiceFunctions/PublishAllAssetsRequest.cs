using System.ComponentModel.DataAnnotations;

namespace Indexer.WebApi.Models.ServiceFunctions
{
    public class PublishAllAssetsRequest
    {
        [Required]
        public string BlockchainId { get; set; }
    }
}
