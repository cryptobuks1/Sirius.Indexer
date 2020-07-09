using System;
using System.Threading.Tasks;
using Grpc.Core;
using Swisschain.Sirius.Indexer.ApiContract.Nonces;

namespace Indexer.GrpcServices
{
    public class NoncesService : Nonces.NoncesBase
    {
        public override Task<GetNonceResponse> GetNonce(GetNonceRequest request, ServerCallContext context)
        {
            throw new NotImplementedException();
        }
    }
}
