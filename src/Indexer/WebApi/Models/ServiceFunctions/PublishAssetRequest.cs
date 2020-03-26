using System.ComponentModel.DataAnnotations;

namespace Indexer.WebApi.Models.ServiceFunctions
{
    public class PublishAssetRequest
    {
        [Required]
        public string AssetId { get; set; }
        [Required]
        public string BlockchainId { get; set; }
        [Required]
        public string Symbol { get; set; }
        public string Address { get; set; }
        public int Accuracy { get; set; }
    }
}
