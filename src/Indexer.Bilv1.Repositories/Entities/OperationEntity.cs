using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Indexer.Bilv1.Repositories.Entities
{
    [Table(name: "operations")]
    public class OperationEntity
    {
        [Key, Column(Order = 0)]
        public string BlockchianId { get; set; }

        [Key, Column(Order = 1)]
        public string BlockchainAssetId { get; set; }

        [Key, Column(Order = 2)]
        public string WalletAddress { get; set; }

        public string OriginalWalletAddress { get; set; }

        [Key, Column(Order = 3)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long OperationId { get; set; }

        public long BlockNumber { get; set; }

        public decimal BalanceChange { get; set; }
    }
}
