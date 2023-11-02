using Azure;
using web_call_recording.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace web_call_recording.Controllers
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
        public async Task<IActionResult> TestCreateCallApi(string userIdentity, string apiVersion = "2023-06-15-preview")
        {
            var _callRes = await acsEndPointTestService.TestAcsCreateCallApi(userIdentity: userIdentity, apiVersion: apiVersion);
            return Ok(_callRes.Content.ReadAsStringAsync().Result);//_callRes
        }

        [HttpPost]
        [Route("TestAcsStartRecordingApi")]
        public async Task<IActionResult> TestAcsStartRecordingApi(string serverCallId, string apiVersion = "2023-06-15-preview")
        {
            var _callRes = await acsEndPointTestService.TestAcsStartRecordingApi(serverCallId: serverCallId, apiVersion: apiVersion);
            return Ok(_callRes.Content.ReadAsStringAsync().Result);///return Ok(_callRes);
        }

        [HttpGet]
        [Route("TestAcsGetRecordingPropertiesApi")]
        public async Task<IActionResult> TestAcsGetRecordingPropertiesApi(string recordingId, string apiVersion = "2023-06-15-preview")
        {
            var _callRes = await acsEndPointTestService.TestAcsGetRecordingPropertiesApi(recordingId: recordingId, apiVersion: apiVersion);
            return Ok(_callRes.Content.ReadAsStringAsync().Result);///return Ok(_callRes);
        }

        [HttpDelete]
        [Route("TestStopRecordingApi")]
        public async Task<IActionResult> TestStopRecordingApi(string recordingId, string apiVersion = "2023-06-15-preview")
        {
            var _callRes = await acsEndPointTestService.TestAcsStopRecordingApi(recordingId: recordingId, apiVersion: apiVersion);
            return Ok(_callRes.Content.ReadAsStringAsync().Result);//Ok(_callRes);
        }

        [HttpPost]
        [Route("TestAcsPauseRecordingApi")]
        public async Task<IActionResult> TestAcsPauseRecordingApi(string recordingId, string apiVersion = "2023-06-15-preview")
        {
            var _callRes = await acsEndPointTestService.TestAcsPauseRecordingApi(recordingId: recordingId, apiVersion: apiVersion);
            return Ok(_callRes.Content.ReadAsStringAsync().Result);//Ok(_callRes);
        }

        [HttpPost]
        [Route("TestAcsResumeRecordingApi")]
        public async Task<IActionResult> TestAcsResumeRecordingApi(string recordingId, string apiVersion = "2023-06-15-preview")
        {
            var _callRes = await acsEndPointTestService.TestAcsResumeRecordingApi(recordingId: recordingId, apiVersion: apiVersion);
            return Ok(_callRes.Content.ReadAsStringAsync().Result);//Ok(_callRes);
        }

    }
}
