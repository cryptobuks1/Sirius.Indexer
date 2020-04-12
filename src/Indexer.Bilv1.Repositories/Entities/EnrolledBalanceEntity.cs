using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Indexer.Bilv1.Repositories.Entities
{
    [Table(name: "enrolled_balance")]
    public class EnrolledBalanceEntity
    {
        [Key, Column(Order = 0)]
        public string BlockchianId { get; set; }

        [Key, Column(Order = 1)]
        public string BlockchainAssetId { get; set; }

        [Key, Column(Order = 2)]
        public string WalletAddress { get; set; }

        public string OriginalWalletAddress { get; set; }

        [Required]
        public long BlockNumber { get; set; }

        public decimal Balance { get; set; }
    }
}
