using Azure.Communication;
using Azure.Communication.CallAutomation;
using Azure.Messaging;
using Azure.Messaging.EventGrid;
using Azure.Messaging.EventGrid.SystemEvents;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

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
        private static string recordingId = "";
        private bool isPauseOnStart;
        private bool isByos;
        private readonly string bringYourOwnStorageUrl;
        private readonly string teamsUser;
        // private static string targetId = "8:acs:19ae37ff-1a44-4e19-aade-198eedddbdf2_0000001b-e3e8-dec7-0d8b-084822007f54";
        public EventsController(ILogger<EventsController> logger
            , IConfiguration configuration,
            CallAutomationClient callAutomationClient)
        {
            //Get ACS Connection String from appsettings.json
            this.hostUrl = configuration.GetValue<string>("BaseUrl");
            this.cognitiveServiceEndpoint = configuration.GetValue<string>("CognitiveServiceEndpoint");
            ArgumentException.ThrowIfNullOrEmpty(this.hostUrl);
            //Call Automation Client
            this.callAutomationClient = callAutomationClient;
            this.logger = logger;
            this.configuration = configuration;
            this.isPauseOnStart = configuration.GetValue<bool>("IsPauseOnStart");
            this.isByos = configuration.GetValue<bool>("IsByos");
            this.bringYourOwnStorageUrl = configuration.GetValue<string>("BringYourOwnStorageUrl");
            this.teamsUser = configuration.GetValue<string>("TeamsUser");
        }

        [HttpPost]
        [Route("createCall")]
        public async Task<IActionResult> CreateCall(string targetId)
        {

            var callbackUri = new Uri(this.hostUrl + $"api/callbacks?callerId={targetId}");
            var target = new CommunicationUserIdentifier(targetId);
            var callInvite = new CallInvite(target);
            var createCallOptions = new CreateCallOptions(callInvite, callbackUri);
            await this.callAutomationClient.CreateCallAsync(createCallOptions);
            return Ok();
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
                    var options = new AnswerCallOptions(incomingCallContext, callbackUri)
                    {
                        CallIntelligenceOptions = new CallIntelligenceOptions
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
                        logger.LogInformation($"Call connected event received for connection id: {answer_result.SuccessResult.CallConnectionId}");
                        var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                        var playTask = HandleVoiceMessageNoteAsync(callConnectionMedia, answer_result.SuccessResult.CallConnectionId);
                        StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(answer_result.SuccessResult.ServerCallId))
                        {
                            RecordingContent = RecordingContent.Audio,
                            RecordingChannel = RecordingChannel.Unmixed,
                            RecordingFormat = RecordingFormat.Wav,
                            PauseOnStart = this.isPauseOnStart,
                            ExternalStorage = this.isByos && !string.IsNullOrEmpty(this.bringYourOwnStorageUrl) ? new BlobStorage(new Uri(this.bringYourOwnStorageUrl)) : null
                        };
                        logger.LogInformation($"Pause On Start-->: {recordingOptions.PauseOnStart}");
                        var recordingTask = this.callAutomationClient.GetCallRecording().StartAsync(recordingOptions);
                        await Task.WhenAll(playTask, recordingTask);
                        recordingId = recordingTask.Result.Value.RecordingId;
                        logger.LogInformation($"Call recording id--> {recordingId}");
                    }
                    this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
                    {
                        logger.LogInformation($"Play completed event received for connection id: {playCompletedEvent.CallConnectionId}");
                        Console.Beep();

                        if (!string.IsNullOrEmpty(this.teamsUser))
                        {
                            var participant = new MicrosoftTeamsUserIdentifier(this.teamsUser);
                            CallInvite callInvite = new CallInvite(participant);
                            await answerCallResult.CallConnection.AddParticipantAsync(callInvite);
                        }

                        await Task.Delay(5000);

                        var state = await this.GetRecordingState(recordingId);

                        if (state == "active")
                        {
                            await this.callAutomationClient.GetCallRecording().PauseAsync(recordingId);
                            logger.LogInformation($"Recording is Paused.");
                            await this.GetRecordingState(recordingId);
                            await Task.Delay(5000);
                            await this.callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
                            logger.LogInformation($"Recording is resumed.");
                        }
                        else
                        {
                            await Task.Delay(5000);
                            await this.callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
                            logger.LogInformation($"Recording is Resumed.");
                            await this.GetRecordingState(recordingId);
                        }

                        await Task.Delay(5000);
                        await this.callAutomationClient.GetCallRecording().StopAsync(recordingId);
                        logger.LogInformation($"Recording is Stopped.");

                        var callConnection = this.callAutomationClient.GetCallConnection(playCompletedEvent.CallConnectionId);

                        await callConnection.HangUpAsync(false);

                    });
                    this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
                    {
                        logger.LogInformation($"Play failed event received for connection id: {playFailedEvent.CallConnectionId}");
                        var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                        var resultInformation = playFailedEvent.ResultInformation;
                        logger.LogError("Encountered error during play, message={msg}, code={code}, subCode={subCode}", resultInformation?.Message, resultInformation?.Code, resultInformation?.SubCode);
                        await this.callAutomationClient.GetCallConnection(playFailedEvent.CallConnectionId).HangUpAsync(true);
                    });

                    this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<TeamsComplianceRecordingStateChanged>(answerCallResult.CallConnection.CallConnectionId, async (teamsComplianceEvent) =>
                    {
                        logger.LogInformation($"Teams Compliance Changed event received for connection id: {teamsComplianceEvent.CallConnectionId}");
                    });

                    this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantSucceeded>(answerCallResult.CallConnection.CallConnectionId, async (teamsComplianceEvent) =>
                    {
                        logger.LogInformation($"AddParticipantSucceeded event received for connection id: {teamsComplianceEvent.CallConnectionId}");
                        logger.LogInformation($"Participant: {teamsComplianceEvent.Participant.RawId.ToString()}");
                    });

                    this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantFailed>(answerCallResult.CallConnection.CallConnectionId, async (teamsComplianceEvent) =>
                    {
                        logger.LogInformation($"AddParticipantFailed event received for connection id: {teamsComplianceEvent.CallConnectionId}");
                        logger.LogInformation($"Message: {teamsComplianceEvent.ResultInformation?.Message.ToString()}");
                    });
                }

                if (eventData is AcsRecordingFileStatusUpdatedEventData statusUpdated)
                {
                    var metadataLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].MetadataLocation;
                    var contentLocation = statusUpdated.RecordingStorageInfo.RecordingChunks[0].ContentLocation;
                    if (!this.isByos)
                    {
                        await this.downloadRecording(contentLocation);
                        await this.DownloadRecordingMetadata(metadataLocation);
                    }
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
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, chatThreadId: {chatThreadId}",
                    parsedEvent.GetType(),
                    parsedEvent.CallConnectionId,
                    parsedEvent.ServerCallId,
                    parsedEvent.OperationContext);
            }

            eventProcessor.ProcessEvents(cloudEvents);
            return Ok();
        }

        private async Task HandleVoiceMessageNoteAsync(CallMedia callConnectionMedia, string callerId)
        {
            try
            {
                string textToPlay = "Sorry, all of our agents are busy on a call. Please leave your phone number and your message after the beep sound.";
                var voiceMessageNote = new TextSource(textToPlay, "en-US-NancyNeural");
                await callConnectionMedia.PlayToAllAsync(voiceMessageNote);
            }
            catch (Exception ex)
            {
                this.logger.LogError($"Exception occured during the play : {ex}");
            }
        }

        private async Task downloadRecording(string contentLocation)
        {
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),"Downloads");    
            var recordingDownloadUri = new Uri(contentLocation);
            var response = await this.callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, $"{downloadsPath}\\test.wav");
        }

        private async Task<string> GetRecordingState(string recordingId)
        {
            var result = await this.callAutomationClient.GetCallRecording().GetStateAsync(recordingId);
            string state = result.Value.RecordingState.ToString();
            logger.LogInformation($"Recording Status:->  {state}");
            logger.LogInformation($"Recording Type:-> { result.Value.RecordingType.ToString()}");
            return state;
        }

        private async Task DownloadRecordingMetadata(string metadataLocation)
        {
            if (!string.IsNullOrEmpty(metadataLocation))
            {
                string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
                var recordingDownloadUri = new Uri(metadataLocation);
                var response = await this.callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, $"{downloadsPath}\\recordingMetadata.json");
            }
            else
            {
                this.logger.LogError("Metadata location is empty.");
            }
        }
    }
}
