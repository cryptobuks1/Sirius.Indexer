using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing;
using IndexerTests.Mocks;
using Shouldly;
using Xunit;

namespace IndexerTests
{
    public class BlockProcessorTests
    {
        private const string BlockchainId = "test-blockchain";

        [Fact]
        public async Task ProcessInitialBlock()
        {
            // arrange
            var initialBlock = new BlockHeader(BlockchainId, number: 10, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow);
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 9, id: "A", previousBlockId: "C", minedAt: DateTime.UtcNow));

            // act
            var output = await processor.ProcessBlock(initialBlock);

            // assert
            output.IndexingDirection.ShouldBe(IndexingDirection.Forward);
        }

        [Fact]
        public async Task ProcessWithoutInitialBlock()
        {
            // arrange
            var block = new BlockHeader(BlockchainId, number: 10, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow);
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);

            // assert
            await processor.ProcessBlock(block).ShouldThrowAsync<NotSupportedException>();
        }

        [Fact]
        public async Task ProcessBlocksInOrder()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)-3(C)-4(D)
            var order = new List<string> { "A", "B", "C", "D" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 10, id: "A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 11, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 12, id: "C", previousBlockId: "B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 13, id: "D", previousBlockId: "C", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 9, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task StoresBlock()
        {
            // arrange
            var initialBlock = new BlockHeader(BlockchainId, number: 10, id: "A", previousBlockId: "Z", minedAt: DateTime.UtcNow);
            var regularBlock = new BlockHeader(BlockchainId, number: 11, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow);
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 9, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            await processor.ProcessBlock(initialBlock);
            await processor.ProcessBlock(regularBlock);

            var readBlock = await blockRepository.GetOrDefault(BlockchainId, 11);

            readBlock.ShouldNotBeNull();
        }

        [Fact]
        public async Task DoesNotStoreUnexpectedBlock()
        {
            // arrange
            var block = new BlockHeader(BlockchainId, number: 10, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow);
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);

            // assert
            await Should.ThrowAsync<NotSupportedException>(() => processor.ProcessBlock(block));

            var readBlock = await blockRepository.GetOrDefault(BlockchainId, 10);

            readBlock.ShouldBeNull();
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber01()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)
            //        \
            // ii:     2(C)-3(D)
            var order = new List<string> { "A", "B", "D", "C", "D" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 1, id: "A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "C", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "D", previousBlockId: "C", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber02()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)-3(D)
            //        \
            // ii:     2(C)-3(E)-4(F)
            var order = new List<string> { "A", "B", "D", "F", "E", "C", "E", "F" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 1, id: "A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "C", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "D", previousBlockId: "B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "E", previousBlockId: "C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "F", previousBlockId: "E", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["D"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber03()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)-3(C)-4(E)-5(H)-6(K)
            //             \
            // ii:          3(D)-4(F)-5(I)-6(L)-7(N)
            //                  \
            // iii:              4(G)-5(J)-6(M)-7(O)-8(P)
            var order = new List<string> { "A", "B", "C", "E", "H", "K", "N", "L", "I", "F", "D", "F", "I", "L", "N", "P", "O", "M", "J", "G", "J", "M", "O", "P" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 1, id: "A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "C", previousBlockId: "B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "D", previousBlockId: "B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "E", previousBlockId: "C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "F", previousBlockId: "D", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "G", previousBlockId: "D", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "H", previousBlockId: "E", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "I", previousBlockId: "F", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "J", previousBlockId: "G", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "K", previousBlockId: "H", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "L", previousBlockId: "I", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "M", previousBlockId: "J", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 7, id: "N", previousBlockId: "L", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 7, id: "O", previousBlockId: "M", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 8, id: "P", previousBlockId: "O", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["K"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["H"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["E"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["C"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["N"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["L"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["I"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["F"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber03NastyVariant()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)-3(C)-4(E)-5(H)-6(K)
            //             \
            // ii:          3(D)-4(F)-5(I)-6(L)-7(N)
            //                  \
            // iii:              4(G)-5(J)-6(M)-7(O)-8(P)
            // in this variant, the switch between branch ii and iii takes place at block number 5 when block J is returned instead of block I
            var order = new List<string> { "A", "B", "C", "E", "H", "K", "N", "L", "J", "G", "D", "G", "J", "M", "O", "P" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 1, id: "A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "C", previousBlockId: "B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "D", previousBlockId: "B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "E", previousBlockId: "C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "F", previousBlockId: "D", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "G", previousBlockId: "D", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "H", previousBlockId: "E", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "I", previousBlockId: "F", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "J", previousBlockId: "G", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "K", previousBlockId: "H", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "L", previousBlockId: "I", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "M", previousBlockId: "J", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 7, id: "N", previousBlockId: "L", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 7, id: "O", previousBlockId: "M", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 8, id: "P", previousBlockId: "O", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["K"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["H"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["E"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["C"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber04()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // case: 3
            // C:       4-5-6
            //         /
            // A: 1-2-3-4
            //       \
            // B:     3-4-5
            var order = new List<string> { "1A", "2A", "3A", "4A", "5B", "4B", "3B", "4B", "5B", "6C", "5C", "4C", "3A", "4C", "5C", "6C" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 1, id: "1A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "2A", previousBlockId: "1A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3A", previousBlockId: "2A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4A", previousBlockId: "3A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3B", previousBlockId: "2A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4B", previousBlockId: "3B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "5B", previousBlockId: "4B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4C", previousBlockId: "3A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "5C", previousBlockId: "4C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "6C", previousBlockId: "5C", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["4A"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["3A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["5B"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["4B"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["3B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber05()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // case: 4
            // C:     3-4-5-6
            //       /
            // A: 1-2-3-4
            //       \
            // B:     3-4-5
            var order = new List<string> { "1A", "2A", "3A", "4A", "5B", "4B", "3B", "4B", "5B", "6C", "5C", "4C", "3C", "4C", "5C", "6C" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 1, id: "1A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "2A", previousBlockId: "1A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3A", previousBlockId: "2A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4A", previousBlockId: "3A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3B", previousBlockId: "2A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4B", previousBlockId: "3B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "5B", previousBlockId: "4B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3C", previousBlockId: "2A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4C", previousBlockId: "3C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "5C", previousBlockId: "4C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "6C", previousBlockId: "5C", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["4A"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["3A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["5B"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["4B"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["3B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber06()
        {
            // arrange
            var blockRepository = new InMemoryBlockHeadersRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // case: 6
            // C:   2-3-4-5-6
            //     /
            // A: 1-2-3-4
            //       \
            // B:     3-4-5
            var order = new List<string> { "1A", "2A", "3A", "4A", "5B", "4B", "3B", "4B", "5B", "6C", "5C", "4C", "3C", "2C", "3C", "4C", "5C", "6C" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 1, id: "1A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "2A", previousBlockId: "1A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3A", previousBlockId: "2A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4A", previousBlockId: "3A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3B", previousBlockId: "2A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4B", previousBlockId: "3B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "5B", previousBlockId: "4B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 2, id: "2C", previousBlockId: "1A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 3, id: "3C", previousBlockId: "2C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 4, id: "4C", previousBlockId: "3C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 5, id: "5C", previousBlockId: "4C", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 6, id: "6C", previousBlockId: "5C", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["4A"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["3A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["5B"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["4B"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["3B"]),
                BlockProcessingResult.CreateBackward(previousBlockHeader: blocks["2A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                results.Add(await processor.ProcessBlock(blocks[id]));
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        private static void AssertResults(List<BlockProcessingResult> results, List<BlockProcessingResult> expectedOutputs)
        {
            results.Count.ShouldBe(expectedOutputs.Count);
            for (var index = 0; index < expectedOutputs.Count; index++)
            {
                var output = results[index];
                var expectedOutput = expectedOutputs[index];

                output.IndexingDirection.ShouldBe(expectedOutput.IndexingDirection);
                output.PreviousBlockHeader?.GlobalId.ShouldBe(expectedOutput.PreviousBlockHeader?.GlobalId);
            }
        }

        private class BlockSet : IEnumerable<BlockHeader>
        {
            private readonly Dictionary<string, BlockHeader> _inner = new Dictionary<string, BlockHeader>();

            public BlockHeader this[string id]
            {
                get => _inner.TryGetValue(id, out var block) ? block : null;
            }

            public void Add(BlockHeader blockHeader) => _inner.Add(blockHeader.Id, blockHeader);

            public IEnumerator<BlockHeader> GetEnumerator() => _inner.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
