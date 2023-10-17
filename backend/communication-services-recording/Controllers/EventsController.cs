using Azure.Messaging;
using Newtonsoft.Json.Linq;
using System.Reflection.Metadata;

namespace communication_services_recording.Controllers
{
    [Route("api")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly CallAutomationClient callAutomationClient;
        private readonly EventConverter eventConverter;
        private readonly ILogger logger;
        private readonly IConfiguration configuration;

        public EventsController(
            ILogger<EventsController> logger,
            IConfiguration configuration,
            ICallRecordingService callRecordingService,
            CallAutomationClient callAutomationClient)
        {
            eventConverter = new EventConverter();
            this.logger = logger;
            this.configuration = configuration;
            this.callAutomationClient = callAutomationClient;
        }

        /* Route for Azure Communication Service eventgrid webhooks*/
        [HttpPost]
        [Route("events")]
        public async Task<IActionResult> Handle([FromBody] EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                logger.LogInformation($"Receieved Call event data : {JsonSerializer.Serialize(eventGridEvent)}");

                // Handle system events
                if (eventGridEvent.TryGetSystemEventData(out object eventData))
                {
                    // Handle the subscription validation event.
                    if (eventData is SubscriptionValidationEventData subscriptionValidationEventData)
                    {
                        var responseData = new SubscriptionValidationResponse
                        {
                            ValidationResponse = subscriptionValidationEventData.ValidationCode
                        };
                        return Ok(responseData);
                    }
                }
                logger.LogInformation("Received  event: {type}", eventGridEvent.EventType);
                var data = eventConverter.Convert(eventGridEvent);
                switch (data)
                {
                    case null:
                        continue;
                    case IncomingCallEvent incomingCallEvent:
                        var callerId = incomingCallEvent?.From?.RawId;
                        var callbackUri = new Uri(this.configuration["BaseUrl"] + $"api/callbacks?callerId={callerId}");
                        var options = new AnswerCallOptions(incomingCallEvent?.IncomingCallContext, callbackUri);

                        AnswerCallResult answerCallResult = await this.callAutomationClient.AnswerCallAsync(options);
                        logger.LogInformation($"Answer call result: {answerCallResult.CallConnection.CallConnectionId}");
                        break;
                    case RecordingFileStatusUpdatedEvent recordingFileStatusUpdated:
                        logger.LogInformation("Call recording file status updated");
                        var recordingFileStatusEvent = eventGridEvent.Data.ToObjectFromJson<RecordingFileStatusUpdatedEvent>();
                        string contentLocation = string.Empty;
                        string documentId = string.Empty;
                        string metadataLocation = string.Empty;
                        foreach (var recordingInfo in recordingFileStatusEvent?.recordingStorageInfo?.recordingChunks)
                        {
                            contentLocation = recordingInfo.contentLocation;
                            documentId = recordingInfo.documentId;
                            metadataLocation = recordingInfo.metadataLocation;
                        }

                        await DownloadRecording(contentLocation, metadataLocation, documentId);
                        break;
                }
            }
            return Ok();
        }

        /* Route for Azure Communication Service eventgrid webhooks*/
        [HttpPost]
        [Route("callbacks")]
        public async Task<IActionResult> Handle([FromBody] CloudEvent[] cloudEvents)
        {
            var eventProcessor = this.callAutomationClient.GetEventProcessor();
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, chatThreadId: {chatThreadId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId,
                    parsedEvent.OperationContext);
            }

            eventProcessor.ProcessEvents(cloudEvents);
            return Ok();
        }
        private async Task DownloadRecording(string contentLocation, string metadataLocation, string documentId)
        {
            var recordingDownloadUri = new Uri(contentLocation);
            string format = await GetFormat(metadataLocation);
            await this.callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, $"{documentId}.{format}");
        }

        private async Task<string> GetFormat(string metadataLocation)
        {
            string format = string.Empty;
            var metaDataDownloadUri = new Uri(metadataLocation);
            var metaDataResponse = await callAutomationClient.GetCallRecording().DownloadStreamingAsync(metaDataDownloadUri);
            using (StreamReader streamReader = new StreamReader(metaDataResponse))
            {
                // Read the JSON content from the stream and parse it into an object
                string jsonContent = await streamReader.ReadToEndAsync();

                // Parse the JSON string
                JObject jsonObject = JObject.Parse(jsonContent);

                // Access the "format" value from the "recordingInfo" object
                format = (string)jsonObject["recordingInfo"]["format"];
            }
            return format;
        }
    }
}
