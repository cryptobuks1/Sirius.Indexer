namespace Swisschain.Sirius.Indexer.MessagingContract
{
    public class SpentCoins
    {
        public string TransactionId { get; set; }
        public int CoinNumber { get; set; }
        public decimal Value { get; set; }
    }
}
