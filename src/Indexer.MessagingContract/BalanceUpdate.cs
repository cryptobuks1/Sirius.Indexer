using System.Collections.Generic;

namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class BalanceUpdate
    {
        public string Address { get; set; }
        public long AssetId { get; set; }
        public IReadOnlyCollection<Transfer> Transfers { get; set; }
        public IReadOnlyCollection<SpentCoins> SpentCoins { get; set; }
        public IReadOnlyCollection<ReceivedCoin> ReceivedCoins { get; set; }
    }
}
