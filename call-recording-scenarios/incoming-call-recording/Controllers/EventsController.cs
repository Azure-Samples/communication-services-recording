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
        private bool isTeamsComplianceUser;
        private bool isRejectCall;
        private bool isCancelAddParticipant;
        private readonly string bringYourOwnStorageUrl;
        private readonly string teamsComplianceUserId;
        private readonly string acsPhonenumber;
        private readonly string targetPhonenumber;
        private readonly string redirectUser;
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
            this.isCancelAddParticipant = configuration.GetValue<bool>("IsCancelAddParticipant");
            this.isByos = configuration.GetValue<bool>("IsByos");
            this.isTeamsComplianceUser = configuration.GetValue<bool>("IsTeamsComplianceUser");
            this.isRejectCall = configuration.GetValue<bool>("IsRejectCall");
            this.bringYourOwnStorageUrl = configuration.GetValue<string>("BringYourOwnStorageUrl");
            this.teamsComplianceUserId = configuration.GetValue<string>("TeamsComplianceUserId");
            this.acsPhonenumber = configuration.GetValue<string>("AcsPhonenumber");
            this.targetPhonenumber = configuration.GetValue<string>("TargetPhonenumber");
            this.redirectUser = configuration.GetValue<string>("RedirectUser");
        }
        string handlePrompt = "Welcome to the Contoso Utilities. Thank you!";
        string pstnUserPrompt = "Hello this is contoso recognition test please confirm or cancel to proceed further.";
        string dtmfPrompt = "Thank you for the update. Please type  one two three four on your keypad to close call.";
        string removeParticipantSucceededPrompt = "RemoveParticipantSucceeded!";
        string confirmLabel = "Confirm";
        string cancelLabel = "Cancel";
        CommunicationUserIdentifier callee;
        [HttpPost]
        [Route("createCall")]
        public async Task<IActionResult> CreateCall(string targetId)
        {

            var callbackUri = new Uri(this.hostUrl + $"api/callbacks?callerId={targetId}");
            callee = new CommunicationUserIdentifier(targetId);
            var callInvite = new CallInvite(callee);
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

                PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);
                PhoneNumberIdentifier caller = new PhoneNumberIdentifier(acsPhonenumber);
                CallInvite callInvite = new CallInvite(target, caller);

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
                    if (this.isRejectCall)
                    {
                        await callAutomationClient.RejectCallAsync(incomingCallContext);
                        logger.LogInformation($"Call Rejected, recject call setting is: {this.isRejectCall}");
                    }
                    else
                    {


                        var options = new AnswerCallOptions(incomingCallContext, callbackUri)
                        {
                            CallIntelligenceOptions = new CallIntelligenceOptions
                            {
                                CognitiveServicesEndpoint = new Uri(this.cognitiveServiceEndpoint)
                            }
                        };

                        AnswerCallResult answerCallResult = await this.callAutomationClient.AnswerCallAsync(options);
                        logger.LogInformation($"Answer call result: {answerCallResult.CallConnection.CallConnectionId}");

                        var callConnectionMedia = answerCallResult.CallConnection.GetCallMedia();
                        //Use EventProcessor to process CallConnected event
                        var answer_result = await answerCallResult.WaitForEventProcessorAsync();


                        if (answer_result.IsSuccess)
                        {
                            logger.LogInformation($"Call connected event received for connection id: {answer_result.SuccessResult.CallConnectionId}");

                            var playTask = HandlePlayAsync(callConnectionMedia, handlePrompt, "handlePromptContext");
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


                            var addParticipantOptions = new AddParticipantOptions(callInvite)
                            {
                                OperationContext = "addPstnUserContext",
                                InvitationTimeoutInSeconds = 10
                            };

                            var addParticipantResult = await answerCallResult.CallConnection.AddParticipantAsync(addParticipantOptions);
                            logger.LogInformation($"Adding Participant to the call: {addParticipantResult.Value?.InvitationId}");

                            // cancel the request with optional parameters
                            if (isCancelAddParticipant)
                            {
                                var cancelAddParticipantOperationOptions = new CancelAddParticipantOperationOptions(addParticipantResult.Value.InvitationId)
                                {
                                    OperationContext = "operationContext",
                                    OperationCallbackUri = new Uri(hostUrl)
                                };
                                await answerCallResult.CallConnection.CancelAddParticipantOperationAsync(cancelAddParticipantOperationOptions);
                                logger.LogInformation($"Cancel Adding Participant to the call");

                            }

                        }

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<RecognizeCompleted>(answerCallResult.CallConnection.CallConnectionId, async (recognizeCompletedEvent) =>
                        {
                            logger.LogInformation($"Recognize completed event received for connection id: {recognizeCompletedEvent.CallConnectionId}");

                            switch (recognizeCompletedEvent.RecognizeResult)
                            {
                                case ChoiceResult choiceResult:
                                    // Take action for Recognition through Choices
                                    var labelDetected = choiceResult.Label;
                                    var phraseDetected = choiceResult.RecognizedPhrase;
                                    logger.LogInformation($"Detected label:--> {labelDetected}");
                                    logger.LogInformation($"Detected phrase:--> {phraseDetected}");
                                    if (labelDetected.ToLower() == confirmLabel.ToLower())
                                    {
                                        logger.LogInformation("Moving towards dtmf test.");
                                        logger.LogInformation("Recognize completed succesfully, labelDetected={labelDetected}, phraseDetected={phraseDetected}", labelDetected, phraseDetected);
                                        await HandleRecognizeAsync(callConnectionMedia, callerId, dtmfPrompt, true);
                                        break;
                                    }
                                    else
                                    {
                                        logger.LogInformation("Moving towards continuous dtmf & send dtmf tones test.");
                                        await StartContinuousDtmfAsync(callConnectionMedia);
                                    }
                                    break;
                                case DtmfResult dtmfResult:
                                    // Take action for Recognition through DTMF
                                    var context = recognizeCompletedEvent.OperationContext;
                                    logger.LogInformation($"Current context-->{context}");
                                    await answerCallResult.CallConnection.RemoveParticipantAsync(CommunicationIdentifier.FromRawId(callerId));

                                    break;
                                case SpeechResult speechResult:
                                    // Take action for Recognition through Choices 
                                    var text = speechResult.Speech;
                                    logger.LogInformation("Recognize completed succesfully, text={text}", text);
                                    break;
                                default:
                                    logger.LogInformation($"Recognize completed succesfully, recognizeResult={recognizeCompletedEvent.RecognizeResult}");
                                    break;
                            }
                        });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<RecognizeFailed>(answerCallResult.CallConnection.CallConnectionId, async (recognizeFailedEvent) =>
                        {
                            logger.LogInformation("Received RecognizeCompleted event");

                        });
                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<PlayCompleted>(answerCallResult.CallConnection.CallConnectionId, async (playCompletedEvent) =>
                        {
                            logger.LogInformation($"Play completed event received for connection id: {playCompletedEvent.CallConnectionId}");
                            Console.Beep();

                            if (this.isTeamsComplianceUser && !string.IsNullOrEmpty(this.teamsComplianceUserId))
                            {
                                var participant = new MicrosoftTeamsUserIdentifier(this.teamsComplianceUserId);
                                CallInvite callInvite = new CallInvite(participant);

                                var addTeamsComplianceUserOptions = new AddParticipantOptions(callInvite)
                                {
                                    OperationContext = "addTeamsComplianceUserContext",
                                    InvitationTimeoutInSeconds = 10
                                };

                                await answerCallResult.CallConnection.AddParticipantAsync(addTeamsComplianceUserOptions);
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
                            var resultInformation = playFailedEvent.ResultInformation;
                            logger.LogError("Encountered error during play, message={msg}, code={code}, subCode={subCode}", resultInformation?.Message, resultInformation?.Code, resultInformation?.SubCode);
                            await this.callAutomationClient.GetCallConnection(playFailedEvent.CallConnectionId).HangUpAsync(true);
                        });



                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantSucceeded>(answerCallResult.CallConnection.CallConnectionId, async (eventData) =>
                        {
                            logger.LogInformation($"AddParticipantSucceeded event received for connection id: {eventData.CallConnectionId}");
                            logger.LogInformation($"Participant:  {JsonSerializer.Serialize(eventData.Participant)}");

                            if (eventData.OperationContext == "addPstnUserContext")
                            {
                                logger.LogInformation("PSTN user added.");

                                var response = await answerCallResult.CallConnection.GetParticipantsAsync();
                                var participantCount = response.Value.Count;
                                var participantList = response.Value;

                                logger.LogInformation($"Total participants in call: {participantCount}");
                                logger.LogInformation($"Participants: {JsonSerializer.Serialize(participantList)}");
                                var muteResponse = await answerCallResult.CallConnection.MuteParticipantAsync(CommunicationIdentifier.FromRawId(callerId));

                                if (muteResponse.GetRawResponse().Status == 200)
                                {
                                    logger.LogInformation("Participant is muted. Waiting for confirmation...");
                                    var participant = await answerCallResult.CallConnection.GetParticipantAsync(CommunicationIdentifier.FromRawId(callerId));
                                    logger.LogInformation($"Is participant muted: {participant.Value.IsMuted}");
                                    logger.LogInformation("Mute participant test completed.");
                                }

                                await HandleRecognizeAsync(callConnectionMedia, callerId, pstnUserPrompt, false);
                            }

                            if (eventData.OperationContext == "addTeamsComplianceUserContext")
                            {
                                logger.LogInformation("Microsoft teams user added.");
                            }
                        });


                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<AddParticipantFailed>(answerCallResult.CallConnection.CallConnectionId, async (eventData) =>
                        {
                            logger.LogInformation($"AddParticipantFailed event received for connection id: {eventData.CallConnectionId}");
                            logger.LogInformation($"Message: {eventData.ResultInformation?.Message.ToString()}");
                        });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<RemoveParticipantSucceeded>(answerCallResult.CallConnection.CallConnectionId, async (eventData) =>
                        {
                            logger.LogInformation($"RemoveParticipantSucceeded event received for connection id: {eventData.CallConnectionId}");
                            logger.LogInformation("Received RemoveParticipantSucceeded event");
                            await HandlePlayAsync(callConnectionMedia, removeParticipantSucceededPrompt, "removeParticipantSucceededPromptContext");
                            // await HandlePlayLoopAsync(callConnectionMedia, removeParticipantSucceededPrompt, "removeParticipantSucceededPromptContext");
                            //await callConnectionMedia.CancelAllMediaOperationsAsync();
                        });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<ContinuousDtmfRecognitionToneReceived>(
                                answerCallResult.CallConnection.CallConnectionId,
                                async (eventData) =>
                                {
                                    logger.LogInformation("Received ContinuousDtmfRecognitionToneReceived event");
                                    logger.LogInformation($"Tone received:--> {eventData.Tone}");
                                    logger.LogInformation($"SequenceId:--> {eventData.SequenceId}");
                                    await StopContinuousDtmfAsync(callConnectionMedia);
                                });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<ContinuousDtmfRecognitionToneFailed>(
                            answerCallResult.CallConnection.CallConnectionId,
                            async (eventData) =>
                            {
                                logger.LogInformation("Received ContinuousDtmfRecognitionToneFailed event");
                                logger.LogInformation($"Message:-->{eventData.ResultInformation.Message}");
                            });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<ContinuousDtmfRecognitionStopped>(
                            answerCallResult.CallConnection.CallConnectionId,
                            async (eventData) =>
                            {
                                logger.LogInformation("Received ContinuousDtmfRecognitionStopped event");
                                await StartSendingDtmfToneAsync(callConnectionMedia);
                            });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<SendDtmfTonesCompleted>(
                            answerCallResult.CallConnection.CallConnectionId,
                            async (eventData) =>
                            {
                                logger.LogInformation("Received SendDtmfTonesCompleted event");
                                await answerCallResult.CallConnection.RemoveParticipantAsync(CommunicationIdentifier.FromRawId(target.RawId));
                                logger.LogInformation($"Send Dtmf tone completed. {target.PhoneNumber} will be removed from call.");
                            });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<SendDtmfTonesFailed>(
                            answerCallResult.CallConnection.CallConnectionId,
                            async (eventData) =>
                            {
                                logger.LogInformation("Received SendDtmfTonesFailed event");
                                logger.LogInformation($"Message:-->{eventData.ResultInformation.Message}");
                            });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<RecordingStateChanged>(
                            answerCallResult.CallConnection.CallConnectionId,
                            async (eventData) =>
                            {
                                logger.LogInformation("Received RecordingStateChanged event");
                            });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<TeamsComplianceRecordingStateChanged>(
                            answerCallResult.CallConnection.CallConnectionId,
                            async (eventData) =>
                            {
                                logger.LogInformation("Received TeamsComplianceRecordingStateChanged event");
                                logger.LogInformation($"CorrelationId:->{eventData.CorrelationId}");
                            });

                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<CallDisconnected>(
                            answerCallResult.CallConnection.CallConnectionId,
                            async (eventData) =>
                            {
                                logger.LogInformation("Received CallDisconnected event");
                            });


                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<RemoveParticipantFailed>(answerCallResult.CallConnection.CallConnectionId, async (eventData) =>
                        {
                            logger.LogInformation($"RemoveParticipantFailed event received for connection id: {eventData.CallConnectionId}");
                            logger.LogInformation("Received RemoveParticipantFailed event");
                        });


                        this.callAutomationClient.GetEventProcessor().AttachOngoingEventProcessor<CallDisconnected>(answerCallResult.CallConnection.CallConnectionId, async (callDisconnectedEvent) =>
                        {
                            logger.LogInformation("Received CallDisconnected event");
                        });
                    }
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
            string downloadsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
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

        private async Task HandleRecognizeAsync(CallMedia callConnectionMedia, string callerId, string message, bool dtmf)
        {
            var choices = GetChoices();

            // Play greeting message
            var greetingPlaySource = new TextSource(message)
            {
                VoiceName = "en-US-NancyNeural"
            };
            PhoneNumberIdentifier target = new PhoneNumberIdentifier(targetPhonenumber);

            var recognizeChoiceOptions =
                new CallMediaRecognizeChoiceOptions(
                    targetParticipant: CommunicationIdentifier.FromRawId(target.RawId), choices)
                {
                    InterruptPrompt = false,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(10),
                    Prompt = greetingPlaySource,
                    OperationContext = "recognizeContext"
                };


            var recognizeDtmfOptions =
               new CallMediaRecognizeDtmfOptions(
                   targetParticipant: CommunicationIdentifier.FromRawId(target.RawId), 4)
               {
                   InterruptPrompt = false,
                   InitialSilenceTimeout = TimeSpan.FromSeconds(15),
                   Prompt = greetingPlaySource,
                   OperationContext = "dtmfContext",
                   InterToneTimeout = TimeSpan.FromSeconds(5)
               };


            CallMediaRecognizeOptions recognizeOptions = dtmf ? recognizeDtmfOptions : recognizeChoiceOptions;

            var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }


        private async Task HandleDtmfRecognizeAsync(CallMedia callConnectionMedia, string callerId, string message, string context)
        {
            // Play greeting message
            var greetingPlaySource = new TextSource(message)
            {
                VoiceName = "en-US-NancyNeural"
            };

            var recognizeOptions =
                new CallMediaRecognizeDtmfOptions(
                    targetParticipant: CommunicationIdentifier.FromRawId(callerId), maxTonesToCollect: 8)
                {
                    InterruptPrompt = false,
                    InterToneTimeout = TimeSpan.FromSeconds(5),
                    Prompt = greetingPlaySource,
                    OperationContext = context,
                    InitialSilenceTimeout = TimeSpan.FromSeconds(15)
                };

            var recognize_result = await callConnectionMedia.StartRecognizingAsync(recognizeOptions);
        }

        private async Task HandlePlayAsync(CallMedia callConnectionMedia, string textToPlay, string context)
        {
            // Play message
            var playSource = new TextSource(textToPlay)
            {
                VoiceName = "en-US-NancyNeural"
            };

            var playOptions = new PlayToAllOptions(playSource) { OperationContext = context };
            await callConnectionMedia.PlayToAllAsync(playOptions);
        }
        private async Task HandlePlayLoopAsync(CallMedia callConnectionMedia, string textToPlay, string context)
        {
            // Play message
            var playSource = new TextSource(textToPlay)
            {
                VoiceName = "en-US-NancyNeural"
            };

            var playOptions = new PlayToAllOptions(playSource) { OperationContext = context, Loop = true };
            await callConnectionMedia.PlayToAllAsync(playOptions);
        }
        private async Task StartContinuousDtmfAsync(CallMedia callMedia)
        {
            await callMedia.StartContinuousDtmfRecognitionAsync(CommunicationIdentifier.FromRawId(targetPhonenumber));
            Console.WriteLine("Continuous Dtmf recognition started. Press one on dialpad.");
        }

        private async Task StopContinuousDtmfAsync(CallMedia callMedia)
        {
            await callMedia.StopContinuousDtmfRecognitionAsync(CommunicationIdentifier.FromRawId(targetPhonenumber));
            Console.WriteLine("Continuous Dtmf recognition stopped. Wait for sending dtmf tones.");
        }

        private async Task StartSendingDtmfToneAsync(CallMedia callMedia)
        {
            List<DtmfTone> tones = new List<DtmfTone>
        {
            DtmfTone.Zero,
            DtmfTone.One
        };

            await callMedia.SendDtmfTonesAsync(tones, CommunicationIdentifier.FromRawId(targetPhonenumber));
            Console.WriteLine("Send dtmf tones started. Respond over phone.");
        }

        private List<RecognitionChoice> GetChoices()
        {
            return new List<RecognitionChoice> {
                new RecognitionChoice("Confirm", new List < string > {
                                      "Confirm",
                                      "First",
                                      "One"
                                    }) {
                                      Tone = DtmfTone.One
                                    },
                    new RecognitionChoice("Cancel", new List < string > {
                      "Cancel",
                      "Second",
                      "Two"
                    }) {
                      Tone = DtmfTone.Two
                        }
                };

        }
    }
}
