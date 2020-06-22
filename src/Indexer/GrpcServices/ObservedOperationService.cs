using System.Threading.Tasks;
using Grpc.Core;
using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Persistence.Entities.ObservedOperations;
using Swisschain.Sirius.Indexer.ApiContract;

namespace Indexer.GrpcServices
{
    public class ObservedOperationService : ObservedOperations.ObservedOperationsBase
    {
        private readonly IObservedOperationsRepository _observedOperationsRepository;

        public ObservedOperationService(IObservedOperationsRepository observedOperationsRepository)
        {
            _observedOperationsRepository = observedOperationsRepository;
        }

        public override async Task<AddObservedOperationResponse> AddObservedOperation(AddObservedOperationRequest request, ServerCallContext context)
        {
            var observedOperation = ObservedOperation.Create(request.OperationId, request.BlockchainId, request.TransactionId);

            await _observedOperationsRepository.AddOrIgnore(observedOperation);

            return new AddObservedOperationResponse();
        }
    }
}
