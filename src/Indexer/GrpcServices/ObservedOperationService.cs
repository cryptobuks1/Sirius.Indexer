using System;
using System.Threading.Tasks;
using Grpc.Core;
using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Persistence;
using Microsoft.Extensions.Logging;
using Swisschain.Sirius.Indexer.ApiContract.ObservedOperations;

namespace Indexer.GrpcServices
{
    public class ObservedOperationService : ObservedOperations.ObservedOperationsBase
    {
        private readonly ILogger<ObservedOperationService> _logger;
        private readonly IBlockchainDbUnitOfWorkFactory _blockchainDbUnitOfWorkFactory;

        public ObservedOperationService(ILogger<ObservedOperationService> logger,
            IBlockchainDbUnitOfWorkFactory blockchainDbUnitOfWorkFactory)
        {
            _logger = logger;
            _blockchainDbUnitOfWorkFactory = blockchainDbUnitOfWorkFactory;
        }

        public override async Task<AddObservedOperationResponse> AddObservedOperation(AddObservedOperationRequest request, ServerCallContext context)
        {
            if (string.IsNullOrWhiteSpace(request.BlockchainId))
            {
                return new AddObservedOperationResponse
                {
                    Error = new ErrorResponseBody
                    {
                        ErrorCode = ErrorResponseBody.Types.ErrorCode.InvalidParameters,
                        ErrorMessage = "Blockchain ID should be not empty"
                    }
                };
            }

            if (string.IsNullOrWhiteSpace(request.TransactionId))
            {
                return new AddObservedOperationResponse
                {
                    Error = new ErrorResponseBody
                    {
                        ErrorCode = ErrorResponseBody.Types.ErrorCode.InvalidParameters,
                        ErrorMessage = "Transaction ID should be not empty"
                    }
                };
            }

            if (request.OperationId == 0)
            {
                return new AddObservedOperationResponse
                {
                    Error = new ErrorResponseBody
                    {
                        ErrorCode = ErrorResponseBody.Types.ErrorCode.InvalidParameters,
                        ErrorMessage = "Operation ID should be not zero"
                    }
                };
            }
            
            var observedOperation = ObservedOperation.Create(request.OperationId, request.BlockchainId, request.TransactionId);

            try
            {
                await using var unitOfWork = await _blockchainDbUnitOfWorkFactory.Start(request.BlockchainId);

                await unitOfWork.ObservedOperations.AddOrIgnore(observedOperation);

                return new AddObservedOperationResponse {Response = new AddObservedOperationResponseBody()};
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Failed to add observed operation {@context}", request);

                return new AddObservedOperationResponse
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
