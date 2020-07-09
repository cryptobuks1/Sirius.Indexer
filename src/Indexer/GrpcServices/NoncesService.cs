using System;
using System.Threading.Tasks;
using Grpc.Core;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.ApiContract.Nonces;

namespace Indexer.GrpcServices
{
    public class NoncesService : Nonces.NoncesBase
    {
        private readonly ILogger<NoncesService> _logger;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;

        public NoncesService(ILogger<NoncesService> logger,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory)
        {
            _logger = logger;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
        }

        public override async Task<GetNonceResponse> GetNonce(GetNonceRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.BlockchainId))
            {
                return new GetNonceResponse
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
                return new GetNonceResponse
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

                var nonceUpdate = await unitOfWork.NonceUpdates.GetLatestOrDefault(request.Address, request.AsAtBlockNumber);

                if (nonceUpdate == null)
                {
                    return new GetNonceResponse
                    {
                        Error = new ErrorResponseBody
                        {
                            ErrorCode = ErrorResponseBody.Types.ErrorCode.AddressNotFound,
                            ErrorMessage = $"Nonce for the address {request.Address} is not found"
                        }
                    };
                }

                return new GetNonceResponse
                {
                    Response = new GetNonceResponseBody
                    {
                        Nonce = nonceUpdate.Nonce
                    }
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to get nonce {@context}", request);

                return new GetNonceResponse
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
