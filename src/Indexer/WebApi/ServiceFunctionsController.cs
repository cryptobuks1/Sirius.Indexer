using System.Threading.Tasks;
using Indexer.Common.ServiceFunctions;
using Indexer.WebApi.Models.ServiceFunctions;
using MassTransit;
using Microsoft.AspNetCore.Mvc;

namespace Indexer.WebApi
{
    [ApiController]
    [Route("api/service-functions")]
    public class ServiceFunctionsController : ControllerBase
    {
        private readonly ISendEndpointProvider _commandsSender;

        public ServiceFunctionsController(ISendEndpointProvider commandsSender)
        {
            _commandsSender = commandsSender;
        }

        [HttpPost("publish-all-assets")]
        public async Task<ActionResult> PublishAllAssets()
        {
            await _commandsSender.Send(new PublishAllAssets());

            return Ok();
        }

        [HttpPost("publish-asset")]
        public async Task<ActionResult> PublishAsset(PublishAssetRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _commandsSender.Send(new PublishAsset
            {
                AssetId = request.AssetId,
                BlockchainId = request.BlockchainId,
                Symbol = request.Symbol,
                Address = request.Address,
                Accuracy = request.Accuracy
            });

            return Ok();
        }
    }
}
