using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain;
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
            var initialBlock = new Block(BlockchainId, number: 10, id: "B", previousBlockId: "A");
            var blockRepository = new InMemoryBlocksRepository();
            var processor = new BlocksProcessor(blockRepository);

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 9, id: "A", previousBlockId: "C"));

            // act
            var output = await processor.ProcessBlock(initialBlock);

            // assert
            output.IndexingDirection.ShouldBe(IndexingDirection.Forward);
        }

        [Fact]
        public async Task ProcessWithoutInitialBlock()
        {
            // arrange
            var block = new Block(BlockchainId, number: 10, id: "B", previousBlockId: "A");
            var blockRepository = new InMemoryBlocksRepository();
            var processor = new BlocksProcessor(blockRepository);

            // assert
            await processor.ProcessBlock(block).ShouldThrowAsync<NotSupportedException>();
        }

        [Fact]
        public async Task ProcessBlocksInOrder()
        {
            // arrange
            var blockRepository = new InMemoryBlocksRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)-3(C)-4(D)
            var order = new List<string> { "A", "B", "C", "D" };
            var blocks = new BlockSet
            {
                new Block(BlockchainId, number: 10, id: "A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 11, id: "B", previousBlockId: "A"),
                new Block(BlockchainId, number: 12, id: "C", previousBlockId: "B"),
                new Block(BlockchainId, number: 13, id: "D", previousBlockId: "C"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 9, id: "Z", previousBlockId: "X"));

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
            var initialBlock = new Block(BlockchainId, number: 10, id: "A", previousBlockId: "Z");
            var regularBlock = new Block(BlockchainId, number: 11, id: "B", previousBlockId: "A");
            var blockRepository = new InMemoryBlocksRepository();
            var processor = new BlocksProcessor(blockRepository);

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 9, id: "Z", previousBlockId: "X"));

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
            var block = new Block(BlockchainId, number: 10, id: "B", previousBlockId: "A");
            var blockRepository = new InMemoryBlocksRepository();
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
            var blockRepository = new InMemoryBlocksRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)
            //        \
            // ii:     2(C)-3(D)
            var order = new List<string> { "A", "B", "D", "C", "D" };
            var blocks = new BlockSet
            {
                new Block(BlockchainId, number: 1, id: "A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 2, id: "B", previousBlockId: "A"),
                new Block(BlockchainId, number: 2, id: "C", previousBlockId: "A"),
                new Block(BlockchainId, number: 3, id: "D", previousBlockId: "C"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 0, id: "Z", previousBlockId: "X"));

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
            var blockRepository = new InMemoryBlocksRepository();
            var processor = new BlocksProcessor(blockRepository);
            var results = new List<BlockProcessingResult>();

            // i: 1(A)-2(B)-3(D)
            //        \
            // ii:     2(C)-3(E)-4(F)
            var order = new List<string> { "A", "B", "D", "F", "E", "C", "E", "F" };
            var blocks = new BlockSet
            {
                new Block(BlockchainId, number: 1, id: "A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 2, id: "B", previousBlockId: "A"),
                new Block(BlockchainId, number: 2, id: "C", previousBlockId: "A"),
                new Block(BlockchainId, number: 3, id: "D", previousBlockId: "B"),
                new Block(BlockchainId, number: 3, id: "E", previousBlockId: "C"),
                new Block(BlockchainId, number: 4, id: "F", previousBlockId: "E"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["D"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 0, id: "Z", previousBlockId: "X"));

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
            var blockRepository = new InMemoryBlocksRepository();
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
                new Block(BlockchainId, number: 1, id: "A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 2, id: "B", previousBlockId: "A"),
                new Block(BlockchainId, number: 3, id: "C", previousBlockId: "B"),
                new Block(BlockchainId, number: 3, id: "D", previousBlockId: "B"),
                new Block(BlockchainId, number: 4, id: "E", previousBlockId: "C"),
                new Block(BlockchainId, number: 4, id: "F", previousBlockId: "D"),
                new Block(BlockchainId, number: 4, id: "G", previousBlockId: "D"),
                new Block(BlockchainId, number: 5, id: "H", previousBlockId: "E"),
                new Block(BlockchainId, number: 5, id: "I", previousBlockId: "F"),
                new Block(BlockchainId, number: 5, id: "J", previousBlockId: "G"),
                new Block(BlockchainId, number: 6, id: "K", previousBlockId: "H"),
                new Block(BlockchainId, number: 6, id: "L", previousBlockId: "I"),
                new Block(BlockchainId, number: 6, id: "M", previousBlockId: "J"),
                new Block(BlockchainId, number: 7, id: "N", previousBlockId: "L"),
                new Block(BlockchainId, number: 7, id: "O", previousBlockId: "M"),
                new Block(BlockchainId, number: 8, id: "P", previousBlockId: "O"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["K"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["H"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["E"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["C"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["N"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["L"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["I"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["F"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 0, id: "Z", previousBlockId: "X"));

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
            var blockRepository = new InMemoryBlocksRepository();
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
                new Block(BlockchainId, number: 1, id: "A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 2, id: "B", previousBlockId: "A"),
                new Block(BlockchainId, number: 3, id: "C", previousBlockId: "B"),
                new Block(BlockchainId, number: 3, id: "D", previousBlockId: "B"),
                new Block(BlockchainId, number: 4, id: "E", previousBlockId: "C"),
                new Block(BlockchainId, number: 4, id: "F", previousBlockId: "D"),
                new Block(BlockchainId, number: 4, id: "G", previousBlockId: "D"),
                new Block(BlockchainId, number: 5, id: "H", previousBlockId: "E"),
                new Block(BlockchainId, number: 5, id: "I", previousBlockId: "F"),
                new Block(BlockchainId, number: 5, id: "J", previousBlockId: "G"),
                new Block(BlockchainId, number: 6, id: "K", previousBlockId: "H"),
                new Block(BlockchainId, number: 6, id: "L", previousBlockId: "I"),
                new Block(BlockchainId, number: 6, id: "M", previousBlockId: "J"),
                new Block(BlockchainId, number: 7, id: "N", previousBlockId: "L"),
                new Block(BlockchainId, number: 7, id: "O", previousBlockId: "M"),
                new Block(BlockchainId, number: 8, id: "P", previousBlockId: "O"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["K"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["H"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["E"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["C"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 0, id: "Z", previousBlockId: "X"));

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
            var blockRepository = new InMemoryBlocksRepository();
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
                new Block(BlockchainId, number: 1, id: "1A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 2, id: "2A", previousBlockId: "1A"),
                new Block(BlockchainId, number: 3, id: "3A", previousBlockId: "2A"),
                new Block(BlockchainId, number: 4, id: "4A", previousBlockId: "3A"),
                new Block(BlockchainId, number: 3, id: "3B", previousBlockId: "2A"),
                new Block(BlockchainId, number: 4, id: "4B", previousBlockId: "3B"),
                new Block(BlockchainId, number: 5, id: "5B", previousBlockId: "4B"),
                new Block(BlockchainId, number: 4, id: "4C", previousBlockId: "3A"),
                new Block(BlockchainId, number: 5, id: "5C", previousBlockId: "4C"),
                new Block(BlockchainId, number: 6, id: "6C", previousBlockId: "5C"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["4A"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["3A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["5B"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["4B"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["3B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 0, id: "Z", previousBlockId: "X"));

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
            var blockRepository = new InMemoryBlocksRepository();
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
                new Block(BlockchainId, number: 1, id: "1A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 2, id: "2A", previousBlockId: "1A"),
                new Block(BlockchainId, number: 3, id: "3A", previousBlockId: "2A"),
                new Block(BlockchainId, number: 4, id: "4A", previousBlockId: "3A"),
                new Block(BlockchainId, number: 3, id: "3B", previousBlockId: "2A"),
                new Block(BlockchainId, number: 4, id: "4B", previousBlockId: "3B"),
                new Block(BlockchainId, number: 5, id: "5B", previousBlockId: "4B"),
                new Block(BlockchainId, number: 3, id: "3C", previousBlockId: "2A"),
                new Block(BlockchainId, number: 4, id: "4C", previousBlockId: "3C"),
                new Block(BlockchainId, number: 5, id: "5C", previousBlockId: "4C"),
                new Block(BlockchainId, number: 6, id: "6C", previousBlockId: "5C"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["4A"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["3A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["5B"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["4B"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["3B"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 0, id: "Z", previousBlockId: "X"));

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
            var blockRepository = new InMemoryBlocksRepository();
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
                new Block(BlockchainId, number: 1, id: "1A", previousBlockId: "Z"),
                new Block(BlockchainId, number: 2, id: "2A", previousBlockId: "1A"),
                new Block(BlockchainId, number: 3, id: "3A", previousBlockId: "2A"),
                new Block(BlockchainId, number: 4, id: "4A", previousBlockId: "3A"),
                new Block(BlockchainId, number: 3, id: "3B", previousBlockId: "2A"),
                new Block(BlockchainId, number: 4, id: "4B", previousBlockId: "3B"),
                new Block(BlockchainId, number: 5, id: "5B", previousBlockId: "4B"),
                new Block(BlockchainId, number: 2, id: "2C", previousBlockId: "1A"),
                new Block(BlockchainId, number: 3, id: "3C", previousBlockId: "2C"),
                new Block(BlockchainId, number: 4, id: "4C", previousBlockId: "3C"),
                new Block(BlockchainId, number: 5, id: "5C", previousBlockId: "4C"),
                new Block(BlockchainId, number: 6, id: "6C", previousBlockId: "5C"),
            };

            var expectedOutputs = new List<BlockProcessingResult>
            {
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["4A"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["3A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["5B"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["4B"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["3B"]),
                BlockProcessingResult.CreateBackward(previousBlock: blocks["2A"]),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
                BlockProcessingResult.CreateForward(),
            };

            await blockRepository.InsertOrIgnore(new Block(BlockchainId, number: 0, id: "Z", previousBlockId: "X"));

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
                output.PreviousBlock?.GlobalId.ShouldBe(expectedOutput.PreviousBlock?.GlobalId);
            }
        }

        private class BlockSet : IEnumerable<Block>
        {
            private readonly Dictionary<string, Block> _inner = new Dictionary<string, Block>();

            public Block this[string id]
            {
                get => _inner.TryGetValue(id, out var block) ? block : null;
            }

            public void Add(Block block) => _inner.Add(block.Id, block);

            public IEnumerator<Block> GetEnumerator() => _inner.Values.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
