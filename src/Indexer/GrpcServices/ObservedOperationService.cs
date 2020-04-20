using System.Threading.Tasks;
using Grpc.Core;
using Indexer.Common.Domain.ObservedOperations;
using Indexer.Common.Persistence.ObservedOperations;
using Swisschain.Extensions.Idempotency;
using Swisschain.Sdk.Server.Common;
using Swisschain.Sirius.Indexer.ApiContract;

namespace Indexer.GrpcServices
{
    public class ObservedOperationService : ObservedOperations.ObservedOperationsBase
    {
        private readonly IObservedOperationsRepository _observedOperationsRepository;
        private readonly IOutboxManager _outboxManager;

        public ObservedOperationService(
            IObservedOperationsRepository observedOperationsRepository,
            IOutboxManager outboxManager)
        {
            _observedOperationsRepository = observedOperationsRepository;
            _outboxManager = outboxManager;
        }

        public override async Task<AddObservedOperationResponse> AddObservedOperation(AddObservedOperationRequest request, ServerCallContext context)
        {
            var outbox = await _outboxManager.Open(request.RequestId, OutboxAggregateIdGenerator.None);

            if (!outbox.IsStored)
            {
                var observedOperation = ObservedOperation.Create(
                    request.OperationsId,
                    request.BlockchainId,
                    request.TransactionId);

                await _observedOperationsRepository.AddOrIgnore(observedOperation);

                outbox.Return(new AddObservedOperationResponse());
            }

            //Should we even send something?
            await _outboxManager.Store(outbox);

            return outbox.GetResponse<AddObservedOperationResponse>();
        }
    }
}
