using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace incoming_call_recording.Controllers
{
    [Route("api")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly ILogger logger;
        private readonly IConfiguration configuration;
        private readonly CallAutomationClient callAutomationClient;
        private string hostUrl = "";
        private string cognitiveServiceEndpoint = "";
        private string transportUrl = "";
        private static string acsRecordingId = "";

        public EventsController(ILogger<EventsController> logger
            , IConfiguration configuration,
            CallAutomationClient callAutomationClient)
        {
            //Get ACS Connection String from appsettings.json
            this.hostUrl = configuration.GetValue<string>("BaseUrl");
            this.cognitiveServiceEndpoint = configuration.GetValue<string>("CognitiveServiceEndpoint");
            this.transportUrl = configuration.GetValue<string>("TransportUrl");
            ArgumentException.ThrowIfNullOrEmpty(this.hostUrl);
            //Call Automation Client
            this.callAutomationClient = callAutomationClient;
            this.logger = logger;
            this.configuration = configuration;
        }

        /* Route for Azure Communication Service eventgrid webhooks*/
        [HttpPost]
        [Route("events")]
        public async Task<IActionResult> Handle([FromBody] EventGridEvent[] eventGridEvents)
        {
            foreach (var eventGridEvent in eventGridEvents)
            {
                logger.LogInformation($"Incoming Call event received : {JsonSerializer.Serialize(eventGridEvent)}");

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
                if (eventData is AcsIncomingCallEventData incomingCallEventData)
                {
                    var callerId = incomingCallEventData?.FromCommunicationIdentifier?.RawId;
                    var incomingCallContext = incomingCallEventData?.IncomingCallContext;
                    var callbackUri = new Uri(hostUrl + $"/api/callbacks/{Guid.NewGuid()}?callerId={callerId}");
                    var mediaStreamingOptions = new MediaStreamingOptions(
                        new Uri(this.transportUrl),
                          MediaStreamingTransport.Websocket,
                          MediaStreamingContent.Audio,
                          MediaStreamingAudioChannel.Mixed
                          );
                    var options = new AnswerCallOptions(incomingCallContext, callbackUri)
                    {
                        MediaStreamingOptions = mediaStreamingOptions,
                        CallIntelligenceOptions = new CallIntelligenceOptions()
                        {
                            CognitiveServicesEndpoint = new Uri(this.cognitiveServiceEndpoint)
                        }
                    };

                    AnswerCallResult answerCallResult = await this.callAutomationClient.AnswerCallAsync(options);
                    logger.LogInformation($"Answer call result: {answerCallResult.CallConnection.CallConnectionId}");

                    //Use EventProcessor to process CallConnected event
                    var answer_result = await answerCallResult.WaitForEventProcessorAsync();
                    if (answer_result.IsSuccess)
                    {
                        logger.LogInformation($"Call connected event received for correlation id: {answer_result.SuccessResult.CorrelationId}");
                        StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(answer_result.SuccessResult.ServerCallId))
                        {
                            RecordingContent = RecordingContent.Audio,
                            RecordingChannel = RecordingChannel.Unmixed,
                            RecordingFormat = RecordingFormat.Wav,
                            RecordingStateCallbackUri = callbackUri
                        };

                        logger.LogInformation($"Starting ACS Recording");

                        var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                        Stopwatch stopwatch = Stopwatch.StartNew();
                        var playTask = HandleVoiceMessageNoteAsync(callConnectionMedia, answer_result.SuccessResult.CallConnectionId, false);
                        var recordingTask = this.callAutomationClient.GetCallRecording().StartAsync(recordingOptions);
                        await Task.WhenAll(playTask, recordingTask);
                        acsRecordingId = recordingTask.Result.Value.RecordingId;
                        logger.LogInformation($"Call recording id: {acsRecordingId}");
                        acsRecordingId = recordingTask.Result.Value.RecordingId;
                    }
                    this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
                    {
                        logger.LogInformation($"Play completed event received for CorrelationId id: {playCompletedEvent.CorrelationId}  time : {DateTime.Now}");
                        Console.Beep();
                    });
                    this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
                    {
                        logger.LogInformation($"Play failed event received for CorrelationId id: {playFailedEvent.CorrelationId} time : {DateTime.Now}");
                        var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                        var resultInformation = playFailedEvent.ResultInformation;
                        logger.LogError("Encountered error during play, message={msg}, code={code}, subCode={subCode}", resultInformation?.Message, resultInformation?.Code, resultInformation?.SubCode);
                    });
                }

                if (eventData is AcsRecordingFileStatusUpdatedEventData statusUpdated)
                {
                    var contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                    await this.downloadRecording(contentLocation);
                }
            }
            return Ok();
        }

        /* Route for Azure Communication Service eventgrid webhooks*/
        [HttpPost]
        [Route("callbacks/{contextid}")]
        public async Task<IActionResult> Handle([FromBody] CloudEvent[] cloudEvents)
        {
            var eventProcessor = this.callAutomationClient.GetEventProcessor();
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, time: {datetime}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId,
                    DateTime.Now);


                if (parsedEvent is RecordingStateChanged recordingStateChanged)
                {
                    if (recordingStateChanged.State == RecordingState.Active)
                    {
                        logger.LogInformation($"Received recording state event time: {recordingStateChanged.State.ToString()}");
                    }
                }
            }

            eventProcessor.ProcessEvents(cloudEvents);
            return Ok();
        }

        private async Task HandleVoiceMessageNoteAsync(CallMedia callConnectionMedia, string callerId, bool isAudioFile = false)
        {
            try
            {
                if (isAudioFile)
                {
                    var prompt = new FileSource(new Uri(hostUrl + "/audio/MainMenu.wav"));
                    await callConnectionMedia.PlayToAllAsync(prompt);
                }
                else
                {
                    string textToPlay = "Sorry, all of our agents are busy on a call. Please leave your phone number and your message after the beep sound.";
                    var voiceMessageNote = new TextSource(textToPlay, "en-US-NancyNeural");
                    await callConnectionMedia.PlayToAllAsync(voiceMessageNote);
                }
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Exception occured during the play : {ex}");
            }
        }

        private async Task downloadRecording(string contentLocation)
        {
            var recordingDownloadUri = new Uri(contentLocation);
            var response = await this.callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, $"test.wav");
        }
    }
}
