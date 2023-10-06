using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace communication_services_recording.Controllers
{
    [Route("api/RestTest")]
    [ApiController]
    public class ACSRestApiCall : Controller
    {
        private readonly ILogger logger;
        private readonly HttpClient _httpClient;

        public ACSRestApiCall(
            ICallRecordingService callRecordingService,
            ILogger<CallRecordingController> logger)
        {

            this.logger = logger;
            this._httpClient = new HttpClient();
        }

        [HttpPost]
        [Route("TestApiCall")]
        public async Task<IActionResult> TestApiCall()
        {
            Target tgt1 = new Target
            {
                kind = "communicationUser",
                communicationUser = new CommunicationUser()
                { id = "8:acs:40b87f1c-e6d1-4772-ba9d-b1360619f38a_0000001b-a48c-5247-6a0b-343a0d00ed34" }
            };

            var requestbody = new MyRequestModel
            {
                targets = new List<Target> { tgt1 },
                callbackUri = "https://4cng02xp-7108.inc1.devtunnels.ms/api/events"
            };


            string callStartApi = @"https://acsrecording.unitedstates.communication.azure.com/calling/callConnections?api-version=2023-06-15-preview";

            var result = CallApiWithJsonBodyAsync(requestbody, callStartApi);

            if (result != null)
            {
                // Process the API response.
                return Ok(result);
            }
            else
            {
                // Handle the error case.
                return BadRequest("Failed to call the API.");
            }
        }

        public class CommunicationUser
        {
            public string id { get; set; }
        }

        public class Target
        {
            public string kind { get; set; }
            public CommunicationUser communicationUser { get; set; }
        }

        public class MyRequestModel
        {
            public List<Target> targets { get; set; }
            public string callbackUri { get; set; }
        }

        private async Task<string> CallApiWithJsonBodyAsync(MyRequestModel requestModel, string _apiUrl)
        {
            try
            {

                var apiUrl = _apiUrl;

                // Serialize the request model to JSON.
                var jsonContent = JsonConvert.SerializeObject(requestModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Bearer Token assignment
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "eyJhbGciOiJSUzI1NiIsImtpZCI6IjVFODQ4MjE0Qzc3MDczQUU1QzJCREU1Q0NENTQ0ODlEREYyQzRDODQiLCJ4NXQiOiJYb1NDRk1kd2M2NWNLOTVjelZSSW5kOHNUSVEiLCJ0eXAiOiJKV1QifQ.eyJza3lwZWlkIjoiYWNzOjQwYjg3ZjFjLWU2ZDEtNDc3Mi1iYTlkLWIxMzYwNjE5ZjM4YV8wMDAwMDAxYi1hNDhjLTUyNDctNmEwYi0zNDNhMGQwMGVkMzQiLCJzY3AiOjE3OTIsImNzaSI6IjE2OTY1OTAzNzYiLCJleHAiOjE2OTY2NzY3NzYsInJnbiI6ImFtZXIiLCJhY3NTY29wZSI6InZvaXAiLCJyZXNvdXJjZUlkIjoiNDBiODdmMWMtZTZkMS00NzcyLWJhOWQtYjEzNjA2MTlmMzhhIiwicmVzb3VyY2VMb2NhdGlvbiI6InVuaXRlZHN0YXRlcyIsImlhdCI6MTY5NjU5MDM3Nn0.ahQc7xGTKKJGiTtzoH71Ky8DgBPWR85tA-6f7DLV5QDgX1M_htDfwiqHILVmSxkHWA-4YZyLE6tWRxVMPkJeUNheA_wNjjJ2IrDZmomRkir25k3tUdTR60GaSfY3bFJ9_2e7u3l7U2_lzCPHN6Ly1Fr5e-kZaCqgUjaUf3CrOxDSeb7JoHpB2H6E65FxPkxh4OEtfmgo4yAH_LhKoKODk04Sl2Tz8oQShH5Z8XvEBgiYkp_Xn-WyPqppyJNgyjhDlBLRO8V4ndLkNDKbBs-WQg7uptOb_OMmWTbgBjXf3R3BMf-Rhhu0nxKQ-xBOpg_MQetpG-utT_6YRLcaYKh0Fg");

                var response = await _httpClient.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    return responseContent;
                }
                else
                {
                    // Handle the error response here.
                    return null;
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions here.
                return null;
            }
        }
    }

}

