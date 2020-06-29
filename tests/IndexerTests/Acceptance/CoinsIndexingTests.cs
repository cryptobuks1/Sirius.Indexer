namespace IndexerTests.Acceptance
{
    public class CoinsIndexingTests
    {
        //[Fact]
        //public async Task CanIndexBtcBlock9727AndSpendItsCoinsByBlock11666()
        //{
        //    var firstPassIndexer = FirstPassIndexer.Restore(
        //        new FirstPassIndexerId("bitcoin", 0),
        //        100_000,
        //        9_727,
        //        1,
        //        DateTime.UtcNow,
        //        DateTime.UtcNow,
        //        0);

        //    var secondPassIndexer = SecondPassIndexer.Restore(
        //        "bitcoin",
        //        9_727,
        //        100_000,
        //        DateTime.UtcNow,
        //        DateTime.UtcNow,
        //        0);

        //    var blocksReader = new BlocksReader();
        //    var primaryBlockProcessor = new PrimaryBlockProcessor();
        //    var coinsPrimaryBlockProcessor = new CoinsPrimaryBlockProcessor();
        //    var inMemoryBus = new InMemoryBus();

        //    var firstPassResult = await firstPassIndexer.IndexNextBlock(
        //        NullLogger<FirstPassIndexer>.Instance,
        //        blocksReader,
        //        primaryBlockProcessor,
        //        coinsPrimaryBlockProcessor,
        //        inMemoryBus);

        //    var secondPassResult = await secondPassIndexer.IndexAvailableBlocks(
        //        NullLogger<SecondPassIndexer>.Instance,
        //        1,
        //        blockHeadersRepository,
        //        appInsight,
        //        coinsSecondaryBlockProcessor);

        //    // TODO: Check intermediate and final balance, coins, transactions, blocks, fees
        //}
    }
}
