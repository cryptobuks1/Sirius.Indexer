using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Indexer.WebApi
{
    [ApiController]
    [Route("api/service-functions")]
    public class ServiceFunctionsController : ControllerBase
    {
        [HttpPost("run-some-service-function")]
        public async Task<ActionResult> RunSomeServiceFunction()
        {
            // TODO:
            await Task.CompletedTask;

            return Ok();
        }
    }
}
