using Azure;
using communication_services_recording.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace communication_services_recording.Controllers
{
    public class AcsEndpointTesterController : Controller
    {
        private readonly IACSEndPointTestService acsEndPointTestService;
        private readonly ILogger logger;

        public AcsEndpointTesterController(IACSEndPointTestService _acsEndPointTestService
            , ILogger<AcsEndpointTesterController> _logger)
        {
            acsEndPointTestService = _acsEndPointTestService;
            logger = _logger;
        }

        [HttpPost]
        [Route("TestCreateCallApi")]
        public async Task<IActionResult> TestCreateCallApi(string apiVersion = "2023-06-15-preview")
        {
            var _callRes = await acsEndPointTestService.TestAcsCreateCallApi(apiVersion);
            return Ok(_callRes);
        }


    }
}
