using Newtonsoft.Json.Linq;

namespace web_call_recording.Controllers
{
    [Route("api")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly CallAutomationClient callAutomationClient;
        private readonly ILogger logger;

        public EventsController(
            ILogger<EventsController> logger,
            CallAutomationClient callAutomationClient)
        {
            this.logger = logger;
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
                if (eventData is AcsRecordingFileStatusUpdatedEventData recordingFileStatusUpdatedEventData)
                {
                    logger.LogInformation("Call recording file status updated");
                    string recordingId = GetRecordingId(eventGridEvent.Subject);
                    var recordingChunks = recordingFileStatusUpdatedEventData.RecordingStorageInfo.RecordingChunks;
                    await DownloadRecording(
                        recordingChunks[0].ContentLocation, 
                        recordingChunks[0].MetadataLocation, 
                        recordingId);
                }
            }

            return Ok();
        }

        private static string GetRecordingId(string recordingId)
        {
            string recordingValue = string.Empty;
            string[] parts = recordingId.Split('/');
            if (parts.Length > 0)
            {
                recordingValue = parts[parts.Length - 1];
            }

            return recordingValue;
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
        private async Task DownloadRecording(string contentLocation, string metadataLocation, string recordingId)
        {
            var recordingDownloadUri = new Uri(contentLocation);
            string format = await GetFormat(metadataLocation);
            await this.callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, $"{recordingId}.{format}");
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
