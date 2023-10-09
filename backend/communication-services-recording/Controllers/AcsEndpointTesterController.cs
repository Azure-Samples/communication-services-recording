using Azure;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace communication_services_recording.Controllers
{
    public class AcsEndpointTesterController : Controller
    {
        [HttpPost]
        [Route("TestCreateCallApi")]
        public async Task<IActionResult> TestCreateCallApi()
        {
            var _callRes = await TestAcsCreateCallApi();
            return Ok(_callRes);
        }

        private async Task<HttpResponseMessage> TestAcsCreateCallApi()
        {
            string resourceEndpoint = "https://acsrecording.unitedstates.communication.azure.com";
            // Create a uri you are going to call.
            var requestUri = new Uri($"{resourceEndpoint}/calling/callConnections?api-version=2023-06-15-preview");
            
            var body = new
            {
                targets = new[]
              {
                new
                {
                    kind = "communicationUser",
                    communicationUser = new
                    {
                        id = "8:acs:40b87f1c-e6d1-4772-ba9d-b1360619f38a_0000001b-b2af-003a-0e04-343a0d0057de"
                    }
                }
               },
                callbackUri = "https://4cng02xp-7108.inc1.devtunnels.ms/api/events"
            };

            var serializedBody = JsonConvert.SerializeObject(body);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };


            // Specify the 'x-ms-date' header as the current UTC timestamp according to the RFC1123 standard
            var date = DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            // Get the host name corresponding with the 'host' header.
            var host = requestUri.Authority;
            // Compute a content hash for the 'x-ms-content-sha256' header.
            var contentHash = ComputeContentHash(serializedBody);

            // Prepare a string to sign.
            var stringToSign = $"POST\n{requestUri.PathAndQuery}\n{date};{host};{contentHash}";
            // Compute the signature.
            var signature = ComputeSignature(stringToSign);
            // Concatenate the string, which will be used in the authorization header.
            var authorizationHeader = $"HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature={signature}";

            // Add a date header.
            requestMessage.Headers.Add("x-ms-date", date);

            // Add a host header.
            // In C#, the 'host' header is added automatically by the 'HttpClient'. However, this step may be required on other platforms such as Node.js.

            // Add a content hash header.
            requestMessage.Headers.Add("x-ms-content-sha256", contentHash);

            // Add an authorization header.
            requestMessage.Headers.Add("Authorization", authorizationHeader);

            string _resp = "";
            HttpResponseMessage response=new HttpResponseMessage();
            try
            {
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = requestUri
                };
                 response = await httpClient.SendAsync(requestMessage);
                //var responseString
                _resp= await response.Content.ReadAsStringAsync();
                Console.WriteLine(_resp);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return response;
        }

        static string ComputeContentHash(string content)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashedBytes);
        }

        static string ComputeSignature(string stringToSign)
        {
            string secret = "CoNkEgq4NOOEHUPtdqmchR8n7SwSFZLAAav6tLJShtIlxCie9jwnrXaUznUV9W4/uV60uaX5wB7ZKftfXvVHLg==";//resourceAccessKey
            using var hmacsha256 = new HMACSHA256(Convert.FromBase64String(secret));
            var bytes = Encoding.UTF8.GetBytes(stringToSign);
            var hashedBytes = hmacsha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashedBytes);
        }

    }
}
