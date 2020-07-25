using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Indexer.Common.Domain.Blocks;
using Indexer.Common.Domain.Indexing.Ongoing;
using Indexer.Common.Persistence.Entities.BlockHeaders;
using IndexerTests.Sdk.Mocks.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using Xunit;

namespace IndexerTests
{
    public class ChainWalkerTests
    {
        private const string BlockchainId = "test-blockchain";

        [Fact]
        public async Task ProcessInitialBlock()
        {
            // arrange
            var initialBlock = new BlockHeader(BlockchainId, number: 10, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow);
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 9, id: "A", previousBlockId: "C", minedAt: DateTime.UtcNow));

            // act
            var output = await walker.MoveTo(initialBlock);

            // assert
            output.Direction.ShouldBe(MovementDirection.Forward);
        }

        [Fact]
        public async Task ProcessWithoutInitialBlock()
        {
            // arrange
            var block = new BlockHeader(BlockchainId, number: 10, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow);
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);

            // assert
            await walker.MoveTo(block).ShouldThrowAsync<NotSupportedException>();
        }

        [Fact]
        public async Task ProcessBlocksInOrder()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

            // i: 1(A)-2(B)-3(C)-4(D)
            var order = new List<string> { "A", "B", "C", "D" };
            var blocks = new BlockSet
            {
                new BlockHeader(BlockchainId, number: 10, id: "A", previousBlockId: "Z", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 11, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 12, id: "C", previousBlockId: "B", minedAt: DateTime.UtcNow),
                new BlockHeader(BlockchainId, number: 13, id: "D", previousBlockId: "C", minedAt: DateTime.UtcNow),
            };

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 9, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        private static async Task IndexNextBlock(IBlockHeadersRepository blocksRepository,
            List<ChainWalkerMovement> results,
            ChainWalker walker,
            BlockSet blocks,
            string id)
        {
            var newBlock = blocks[id];
            var result = await walker.MoveTo(newBlock);
            
            if (result.Direction == MovementDirection.Forward)
            {
                await blocksRepository.InsertOrIgnore(newBlock);
            }
            else if (result.Direction == MovementDirection.Backward)
            {
                await blocksRepository.Remove(result.EvictedBlockHeader.Id);
            }

            results.Add(result);
        }

        [Fact]
        public async Task DoesNotStoreUnexpectedBlock()
        {
            // arrange
            var block = new BlockHeader(BlockchainId, number: 10, id: "B", previousBlockId: "A", minedAt: DateTime.UtcNow);
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;

            // assert
            await Should.ThrowAsync<NotSupportedException>(() => walker.MoveTo(block));

            var readBlock = await blocksRepository.GetOrDefault(10);

            readBlock.ShouldBeNull();
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber01()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

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

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["B"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber02()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

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

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["D"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["B"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber03()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

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

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["K"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["H"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["E"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["C"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["N"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["L"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["I"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["F"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber03NastyVariant()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

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

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["K"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["H"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["E"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["C"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber04()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

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

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["4A"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["3A"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["5B"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["4B"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["3B"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber05()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

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

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["4A"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["3A"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["5B"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["4B"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["3B"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        [Fact]
        public async Task ProcessBlocksChallengeNumber06()
        {
            // arrange
            var unitOfWorkFactory = new TestBlockchainDbUnitOfWorkFactory();
            var walker = new ChainWalker(NullLogger<ChainWalker>.Instance, unitOfWorkFactory);
            var blocksRepository = unitOfWorkFactory.UnitOfWork.BlockHeaders;
            var results = new List<ChainWalkerMovement>();

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

            var expectedOutputs = new List<ChainWalkerMovement>
            {
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["4A"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["3A"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["5B"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["4B"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["3B"]),
                ChainWalkerMovement.CreateBackward(evictedBlockHeader: blocks["2A"]),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
                ChainWalkerMovement.CreateForward(),
            };

            await blocksRepository.InsertOrIgnore(new BlockHeader(BlockchainId, number: 0, id: "Z", previousBlockId: "X", minedAt: DateTime.UtcNow));

            // act
            foreach (var id in order)
            {
                await IndexNextBlock(blocksRepository, results, walker, blocks, id);
            }

            // assert
            AssertResults(results, expectedOutputs);
        }

        private static void AssertResults(List<ChainWalkerMovement> results, List<ChainWalkerMovement> expectedOutputs)
        {
            results.Count.ShouldBe(expectedOutputs.Count);
            for (var index = 0; index < expectedOutputs.Count; index++)
            {
                var output = results[index];
                var expectedOutput = expectedOutputs[index];

                output.Direction.ShouldBe(expectedOutput.Direction);
                output.EvictedBlockHeader?.BlockchainId.ShouldBe(expectedOutput.EvictedBlockHeader?.BlockchainId);
                output.EvictedBlockHeader?.Id.ShouldBe(expectedOutput.EvictedBlockHeader?.Id);
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
