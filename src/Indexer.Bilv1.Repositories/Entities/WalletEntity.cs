using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Indexer.Bilv1.Repositories.Entities
{
    [Table(name: "wallets")]
    public class WalletEntity
    {
        [Key, Column(Order = 0)]
        public string BlockchainId { get; set; }

        [Key, Column(Order = 1)]
        public string NetworkId { get; set; }

        //Should be in lower case
        [Key, Column(Order = 2)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        //Index
        public string WalletAddress { get; set; }

        public string OriginalWalletAddress { get; set; }

        public string PublicKey { get; set; }

        public bool IsCompromised { get; set; }

        public DateTime ImportedDateTime { get; set; }
    }
}
