using System;
using System.Linq;
using System.Threading.Tasks;
using Grpc.Core;
using Indexer.Common.Persistence.Entities.UnspentCoins;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.ApiContract.UnspentCoins;

namespace Indexer.GrpcServices
{
    public class UnspentCoinsService : UnspentCoins.UnspentCoinsBase
    {
        private readonly ILogger<UnspentCoinsService> _logger;
        private readonly IUnspentCoinsRepository _unspentCoinsRepository;

        public UnspentCoinsService(ILogger<UnspentCoinsService> logger, IUnspentCoinsRepository unspentCoinsRepository)
        {
            _logger = logger;
            _unspentCoinsRepository = unspentCoinsRepository;
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
                var unspentCoins = await _unspentCoinsRepository.GetByAddress(request.Address,
                    request.AsAtBlockNumber);



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
