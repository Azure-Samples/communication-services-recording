using Newtonsoft.Json;

namespace communication_services_recording.Services
{
    public class ACSEndPointTestService : IACSEndPointTestService
    {
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public ACSEndPointTestService(IConfiguration _configuration,
             ILogger<ACSEndPointTestService> logger)
        {
            this.configuration = _configuration;
            this.logger = logger;
        }

        public async Task<HttpResponseMessage> TestAcsCreateCallApi(string userIdentity, string apiVersion = "")
        {
            string resourceEndpoint = configuration["BaseUrl"];

            string callconnectRequestUri = $"{resourceEndpoint}calling/callConnections?api-version={apiVersion}";

            var requestUri = new Uri($"{callconnectRequestUri}");

            var body = new
            {
                targets = new[]
              {
                    new
                    {
                        kind = "communicationUser",
                        communicationUser = new
                        {
                            id = userIdentity
                        }
                    }
                },
                callbackUri = configuration["CallbackUri"]
            };

            var serializedBody = JsonConvert.SerializeObject(body);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };

            return await PerformRequestHandling(callconnectRequestUri, requestMessage, serializedBody);
        }

        public async Task<HttpResponseMessage> TestAcsStartRecordingApi(string serverCallId, string apiVersion = "")
        {
            string resourceEndpoint = configuration["BaseUrl"];
            //POST {endpoint}/calling/recordings?api-version=2023-06-15-preview
            string callconnectRequestUri = $"{resourceEndpoint}calling/recordings?api-version={apiVersion}";
            var requestUri = new Uri($"{callconnectRequestUri}");

            var body = new
            {
                recordingStateCallbackUri = configuration["CallbackUri"],
                recordingContentType = (object)null,
                recordingChannelType = (object)null,
                recordingFormatType = (object)null,
                callLocator = new
                {
                    serverCallId = serverCallId,
                    kind = "serverCallLocator"
                }
            };

            var serializedBody = JsonConvert.SerializeObject(body);

            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };
            return await PerformRequestHandling(callconnectRequestUri, requestMessage, serializedBody);
        }
        public async Task<HttpResponseMessage> TestAcsGetRecordingPropertiesApi(string apiVersion = "", string recordingId = "")
        {
            string resourceEndpoint = configuration["BaseUrl"];
            string callconnectRequestUri = $"{resourceEndpoint}calling/recordings/{recordingId}?api-version={apiVersion}";
            var requestUri = new Uri($"{callconnectRequestUri}");
            //var body = "";
            var serializedBody = "";
            var requestMessage = new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };
            return await PerformRequestHandling(callconnectRequestUri, requestMessage, serializedBody);
        }


        public async Task<HttpResponseMessage> TestAcsStopRecordingApi(string apiVersion = "", string recordingId = "")
        {
            string resourceEndpoint = configuration["BaseUrl"];
            string callconnectRequestUri = $"{resourceEndpoint}calling/recordings/{recordingId}?api-version={apiVersion}";
            var requestUri = new Uri($"{callconnectRequestUri}");
            //var body = "";
            var serializedBody = "";
            var requestMessage = new HttpRequestMessage(HttpMethod.Delete, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };
            return await PerformRequestHandling(callconnectRequestUri, requestMessage, serializedBody);
        }

        public async Task<HttpResponseMessage> TestAcsPauseRecordingApi(string apiVersion = "", string recordingId = "")
        {
            string resourceEndpoint = configuration["BaseUrl"];
            string callconnectRequestUri = $"{resourceEndpoint}calling/recordings/{recordingId}:pause?api-version={apiVersion}";
            var requestUri = new Uri($"{callconnectRequestUri}");
            //var body = "";
            var serializedBody = "";
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };
            return await PerformRequestHandling(callconnectRequestUri, requestMessage, serializedBody);
        }

        public async Task<HttpResponseMessage> TestAcsResumeRecordingApi(string apiVersion = "", string recordingId = "")
        {
            string resourceEndpoint = configuration["BaseUrl"];
            string callconnectRequestUri = $"{resourceEndpoint}calling/recordings/{recordingId}:resume?api-version={apiVersion}";
            var requestUri = new Uri($"{callconnectRequestUri}");
            //var body = "";
            var serializedBody = "";
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, requestUri)
            {
                Content = new StringContent(serializedBody, Encoding.UTF8, "application/json")
            };
            return await PerformRequestHandling(callconnectRequestUri, requestMessage, serializedBody);
        }

        private async Task<HttpResponseMessage> PerformRequestHandling(string reqUri, HttpRequestMessage requestMessage, string serializedBody)
        {
            //string resourceEndpoint = configuration["BaseUrl"];
            var requestUri = new Uri($"{reqUri}");
            // Specify the 'x-ms-date' header as the current UTC timestamp according to the RFC1123 standard
            var date = DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture);
            // Get the host name corresponding with the 'host' header.
            var host = requestUri.Authority;
            // Compute a content hash for the 'x-ms-content-sha256' header.
            var contentHash = ComputeContentHash(serializedBody);
            // Prepare a string to sign. -> //$"POST\n{requestUri.PathAndQuery}\n{date};{host};{contentHash}";
            var stringToSign = $"POST\n{requestUri.PathAndQuery}\n{date};{host};{contentHash}";
            //$"POST\n{requestUri.PathAndQuery}\n{date};{host};{contentHash}";
            // Compute the signature.
            var signature = ComputeSignature(stringToSign);
            // Concatenate the string, which will be used in the authorization header.
            var authorizationHeader = $"HMAC-SHA256 SignedHeaders=x-ms-date;host;x-ms-content-sha256&Signature={signature}";
            // Add a date header.
            requestMessage.Headers.Add("x-ms-date", date);
            // Add a content hash header.
            requestMessage.Headers.Add("x-ms-content-sha256", contentHash);
            // Add an authorization header.
            requestMessage.Headers.Add("Authorization", authorizationHeader);
            string _resp = "";
            HttpResponseMessage response = new HttpResponseMessage();
            try
            {
                HttpClient httpClient = new HttpClient
                {
                    BaseAddress = requestUri
                };
                response = await httpClient.SendAsync(requestMessage);
                //var responseString
                _resp = await response.Content.ReadAsStringAsync();
                Console.WriteLine(_resp);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error in request handling!");
            }
            return response;
        }
        private string ComputeContentHash(string content)
        {
            using var sha256 = SHA256.Create();
            byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(content));
            return Convert.ToBase64String(hashedBytes);
        }
        private string ComputeSignature(string stringToSign)
        {
            string secret = configuration["AcsKey"];
            using var hmacsha256 = new HMACSHA256(Convert.FromBase64String(secret));
            var bytes = Encoding.UTF8.GetBytes(stringToSign);
            var hashedBytes = hmacsha256.ComputeHash(bytes);
            return Convert.ToBase64String(hashedBytes);
        }
    }
}
