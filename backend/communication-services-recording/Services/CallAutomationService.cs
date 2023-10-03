using Azure.Communication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Threading;

namespace communication_services_recording.Services
{
    public class CallAutomationService : ICallAutomationService
    {
        private readonly CallAutomationClient callAutomationClient;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;
        private const string callbackurl = "/api/callbacks";
        private const string baseUrl = "";
        public CallAutomationService(
            IConfiguration configuration,
            ILogger<CallAutomationService> logger)
        {
            this.configuration = configuration;
            this.logger = logger;

            var acsConnectionString = configuration["AcsConnectionString"];
            var pmaUrl = configuration["pmaUrl"];
            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);
            // (pmaUrl != null) ? new CallAutomationClient(pmaEndpoint: new Uri(pmaUrl), acsConnectionString) 
            this.callAutomationClient = new CallAutomationClient(acsConnectionString);
        }

        public async Task<CreateCallResult> CreateCall(string callerId, string targetId)
        {
            try
            {
                var callbackUri = new Uri(
                    baseUri: new Uri(baseUrl),
                    relativeUri: "/api/callbacks" + $"?targetParticipant={targetId}");
                var target = new PhoneNumberIdentifier(targetId);
                var caller = new PhoneNumberIdentifier(callerId);
                var callInvite = new CallInvite(target, caller);
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri);
                return await callAutomationClient.CreateCallAsync(createCallOptions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Could not create outbound call");
                throw;
            }
        }
    }
}
