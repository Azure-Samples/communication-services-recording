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
        private string transportUrl = "";
        private string targetPhoneNumber = "";
        private string acsPhoneNumber = "";
        private string acsTargetUser = "";
        private static string acsRecordingId = "";

        public EventsController(ILogger<EventsController> logger
            , IConfiguration configuration,
            CallAutomationClient callAutomationClient)
        {
            //Get ACS Connection String from appsettings.json
            this.hostUrl = configuration.GetValue<string>("BaseUrl");
            this.cognitiveServiceEndpoint = configuration.GetValue<string>("CognitiveServiceEndpoint");
            this.transportUrl = configuration.GetValue<string>("TransportUrl");
            this.acsPhoneNumber = configuration.GetValue<string>("AcsPhoneNumber");
            this.targetPhoneNumber = configuration.GetValue<string>("TargetPhoneNumber");
            this.acsTargetUser = configuration.GetValue<string>("AcsTargetUser");
            ArgumentException.ThrowIfNullOrEmpty(this.hostUrl);
            //Call Automation Client
            this.callAutomationClient = callAutomationClient;
            this.logger = logger;
            this.configuration = configuration;
        }

        [HttpPost]
        [Route("createOutBoundCall")]
        public async Task<IActionResult> CreatePSTNCall()
        {
            PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhoneNumber);
            PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhoneNumber);
            var callbackUri = new Uri(this.hostUrl + $"/api/callbacks");
            var mediaStreamingOptions = new MediaStreamingOptions(
                        new Uri(this.transportUrl),
                          MediaStreamingTransport.Websocket,
                          MediaStreamingContent.Audio,
                          MediaStreamingAudioChannel.Mixed
                          );
            CallInvite callInvite = new CallInvite(target, caller);
            var createCallOptions = new CreateCallOptions(callInvite, callbackUri)
            {
                CallIntelligenceOptions = new CallIntelligenceOptions() { CognitiveServicesEndpoint = new Uri(cognitiveServiceEndpoint) },
                MediaStreamingOptions = mediaStreamingOptions
            };

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
                    //var mediaStreamingOptions = new MediaStreamingOptions(
                    //    new Uri(this.transportUrl),
                    //      MediaStreamingContent.Audio,
                    //      MediaStreamingAudioChannel.Mixed,
                    //      MediaStreamingTransport.Websocket
                    //      );
                    //var options = new AnswerCallOptions(incomingCallContext, callbackUri)
                    //{
                    //    MediaStreamingOptions = mediaStreamingOptions,
                    //    CallIntelligenceOptions = new CallIntelligenceOptions()
                    //    {
                    //        CognitiveServicesEndpoint = new Uri(this.cognitiveServiceEndpoint)
                    //    }
                    //};

                    //AnswerCallResult answerCallResult = await this.callAutomationClient.AnswerCallAsync(options);
                    //logger.LogInformation($"Answer call result: {answerCallResult.CallConnection.CallConnectionId}");

                    ////Use EventProcessor to process CallConnected event
                    //var answer_result = await answerCallResult.WaitForEventProcessorAsync();
                    //if (answer_result.IsSuccess)
                    //{
                    //    logger.LogInformation($"Call connected event received for correlation id: {answer_result.SuccessResult.CorrelationId}");
                    //    StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(answer_result.SuccessResult.ServerCallId))
                    //    {
                    //        RecordingContent = RecordingContent.Audio,
                    //        RecordingChannel = RecordingChannel.Unmixed,
                    //        RecordingFormat = RecordingFormat.Wav,
                    //        RecordingStateCallbackUri = callbackUri
                    //    };

                    //    logger.LogInformation($"Starting ACS Recording");

                    //    var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                    //    await HandleVoiceMessageNoteAsync(callConnectionMedia, answer_result.SuccessResult.CallConnectionId, false);
                    //    var recordingResult = await this.callAutomationClient.GetCallRecording().StartAsync(recordingOptions);

                    //    acsRecordingId = recordingResult.Value.RecordingId;
                    //    logger.LogInformation($"Call recording id: {acsRecordingId}");
                    //}

                    //this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantSucceeded>(answerCallResult.CallConnection.CallConnectionId, async (eventData) =>
                    //{
                    //    logger.LogInformation($"AddParticipantSucceeded event received for connection id: {eventData.CallConnectionId}");
                    //    logger.LogInformation($"Participant:  {JsonSerializer.Serialize(eventData.Participant)}");
                    //});

                    //this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantFailed>(answerCallResult.CallConnection.CallConnectionId, async (eventData) =>
                    //{
                    //    logger.LogInformation($"AddParticipantFailed event received for connection id: {eventData.CallConnectionId}");
                    //    logger.LogInformation($"Message: {eventData.ResultInformation?.Message.ToString()}");
                    //});

                    //this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
                    //{
                    //    logger.LogInformation($"Play completed event received for CorrelationId id: {playCompletedEvent.CorrelationId}  time : {DateTime.Now}");
                    //    Console.Beep();
                    //});
                    //this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayFailed>(answerCallResult.CallConnection.CallConnectionId, async (playFailedEvent) =>
                    //{
                    //    logger.LogInformation($"Play failed event received for CorrelationId id: {playFailedEvent.CorrelationId} time : {DateTime.Now}");
                    //    var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                    //    var resultInformation = playFailedEvent.ResultInformation;
                    //    logger.LogError("Encountered error during play, message={msg}, code={code}, subCode={subCode}", resultInformation?.Message, resultInformation?.Code, resultInformation?.SubCode);
                    //});
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
        [Route("callbacks")]
        //[Route("callbacks/{contextid}")]
        public async Task<IActionResult> Handle([FromBody] CloudEvent[] cloudEvents)
        {
            var eventProcessor = this.callAutomationClient.GetEventProcessor();
            foreach (var cloudEvent in cloudEvents)
            {
                CallAutomationEventBase parsedEvent = CallAutomationEventParser.Parse(cloudEvent);
                logger.LogInformation(
                    "Received call event: {type}, callConnectionID: {connId}, serverCallId: {serverId}, time: {datetime}",
                    parsedEvent?.GetType(),
                    parsedEvent?.CallConnectionId,
                    parsedEvent?.ServerCallId,
                    DateTime.Now);


                if (parsedEvent is RecordingStateChanged recordingStateChanged)
                {
                    if (recordingStateChanged.State == RecordingState.Active)
                    {
                        logger.LogInformation($"Received recording state event time: {recordingStateChanged.State.ToString()}");
                    }
                }
                else if (parsedEvent is CallConnected callConnected)
                {
                    logger.LogInformation($"Received CallConnected event ");

                    var acsTarget = new CommunicationUserIdentifier(acsTargetUser);
                    CallInvite callInvite = new CallInvite(acsTarget);

                    var addParticipantOptions = new AddParticipantOptions(callInvite)
                    {
                        InvitationTimeoutInSeconds = 30
                    };
                    var addParticipantResult = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId).AddParticipantAsync(addParticipantOptions);
                    logger.LogInformation($"Adding Participant to the call: {addParticipantResult.Value?.InvitationId}");
                }
                else if (parsedEvent is AddParticipantSucceeded addParticipantSucceeded)
                {
                    logger.LogInformation($"Received AddParticipantSucceeded event ");
                    var response = await callAutomationClient.GetCallConnection(parsedEvent.CallConnectionId).GetParticipantsAsync();
                    var participantCount = response.Value.Count;
                    var participantList = response.Value;

                    logger.LogInformation($"Total participants in call: {participantCount}");
                    logger.LogInformation($"Participants: {JsonSerializer.Serialize(participantList)}");
                }
                else if (parsedEvent is AddParticipantFailed addParticipantFailed)
                {
                    logger.LogInformation($"Received AddParticipantFailed event ");
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
