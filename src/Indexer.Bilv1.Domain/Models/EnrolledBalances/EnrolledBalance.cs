namespace Indexer.Bilv1.Domain.Models.EnrolledBalances
{
    public sealed class EnrolledBalance
    {
        public DepositWalletKey Key { get; }
        public decimal Balance { get; set; }
        public long Block { get; set; }

        private EnrolledBalance(
            DepositWalletKey key,
            decimal balance,
            long block)
        {
            Balance = balance;
            Key = key;
            Block = block;
        }

        public static EnrolledBalance Create(DepositWalletKey key, decimal balance, long block)
        {
            return new EnrolledBalance(key, balance, block);
        }
    }
}
