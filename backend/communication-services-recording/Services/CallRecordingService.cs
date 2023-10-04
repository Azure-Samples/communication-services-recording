

namespace communication_services_recording.Services
{
    public class CallRecordingService : ICallRecordingService
    {
        private readonly CallAutomationClient callAutomationClient;
        private readonly IConfiguration configuration;
        private readonly ILogger logger;

        public CallRecordingService(
            IConfiguration configuration,
            ILogger<CallRecordingService> logger,
            CallAutomationClient callAutomationClient)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.callAutomationClient = callAutomationClient;
        }

        public async Task<CreateCallResult> CreateCallAsync(string targetId)
        {
            try
            {
                var callbackUri = new Uri(this.configuration["BaseUrl"] + $"api/callbacks?targetId={targetId}");
                var target = new CommunicationUserIdentifier(targetId);
                var callInvite = new CallInvite(target);
                var createCallOptions = new CreateCallOptions(callInvite, callbackUri);
                return await callAutomationClient.CreateCallAsync(createCallOptions);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Couldnt create the call");
                throw;
            }
        }

        public async Task<RecordingStateResult> StartRecording(string serverCallId)
        {
            try
            {
                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
                recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
                recordingOptions.RecordingContent = RecordingContent.Audio;
                recordingOptions.RecordingFormat = RecordingFormat.Wav;
                var startRecordingResponse = await this.callAutomationClient.GetCallRecording()
                    .StartAsync(recordingOptions).ConfigureAwait(false);
                return startRecordingResponse;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Couldnt start the recording");
                throw;
            }
        }

        public async Task<RecordingStateResult> StartRecording(RecordingRequest recordingRequest)
        {
            try
            {
                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(recordingRequest.ServerCallId));
                
                /*TODO validate recording channel, content and format if its invalid assign the default*/
                
                recordingOptions.RecordingChannel = recordingRequest.RecordingChannel;
                recordingOptions.RecordingContent = recordingRequest.RecordingContent;
                recordingOptions.RecordingFormat = recordingRequest.RecordingFormat;
                var startRecordingResponse = await this.callAutomationClient.GetCallRecording()
                    .StartAsync(recordingOptions).ConfigureAwait(false);
                return startRecordingResponse;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Couldnt start the recording");
                throw;
            }
        }

        public async Task StopRecording(string recordingId)
        {
            await this.callAutomationClient.GetCallRecording().StopAsync(recordingId);
        }

        public Task PauseRecording(string recordingId)
        {
            throw new NotImplementedException();
        }

        public Task ResumeRecording(string recordingId)
        {
            throw new NotImplementedException();
        }
    }
}
