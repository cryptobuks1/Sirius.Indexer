using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.ApiContract.UnspentCoins;

namespace Indexer.GrpcServices
{
    public class UnspentCoinsService : UnspentCoins.UnspentCoinsBase
    {
        private readonly ILogger<UnspentCoinsService> _logger;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;

        public UnspentCoinsService(ILogger<UnspentCoinsService> logger, 
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory)
        {
            _logger = logger;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
        }

        public override async Task<GetUnspentCoinsResponse> GetUnspentCoins(GetUnspentCoinsRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.BlockchainId))
            {
                return new GetUnspentCoinsResponse
                {
                    Error = new ErrorResponseBody
                    {
                        ErrorCode = ErrorResponseBody.Types.ErrorCode.InvalidParameters,
                        ErrorMessage = "Blockchain ID should be not empty"
                    }
                };
            }

            if (string.IsNullOrWhiteSpace(request.Address))
            {
                return new GetUnspentCoinsResponse
                {
                    Error = new ErrorResponseBody
                    {
                        ErrorCode = ErrorResponseBody.Types.ErrorCode.InvalidParameters,
                        ErrorMessage = "Address ID should be not empty"
                    }
                };
            }

            try
            {
                await using var unitOfWork = await _blockchainDbUnitOfWorkFactory.Start(request.BlockchainId);

                var unspentCoins = await unitOfWork.UnspentCoins.GetByAddress(request.Address, request.AsAtBlockNumber);
                
                return new GetUnspentCoinsResponse
                {
                    Response = new GetUnspentCoinsResponseBody
                    {
                        UnspentCoins =
                        {
                            unspentCoins.Select(x => new UnspentCoin
                            {
                                Id = x.Id,
                                Address = x.Address,
                                ScriptPubKey = x.ScriptPubKey,
                                Unit = x.Unit
                            })
                        }
                    }
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get unspent coins {@context}", request);

                return new GetUnspentCoinsResponse
                {
                    Error = new ErrorResponseBody
                    {
                        ErrorCode = ErrorResponseBody.Types.ErrorCode.Unknown,
                        ErrorMessage = e.Message
                    }
                };
            }
        }
    }
}
