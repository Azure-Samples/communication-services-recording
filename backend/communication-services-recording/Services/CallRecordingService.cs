

using Azure;

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

        public async Task<RecordingStateResult> StartRecording(RecordingRequest recordingRequest)
        {
            try
            {
                StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(recordingRequest.ServerCallId));
                recordingOptions.RecordingChannel = !string.IsNullOrWhiteSpace(recordingRequest.RecordingChannel) ?
                    new RecordingChannel(recordingRequest.RecordingChannel) :
                    RecordingChannel.Unmixed;

                recordingOptions.RecordingContent = !string.IsNullOrWhiteSpace(recordingRequest.RecordingContent) ?
                    new RecordingContent(recordingRequest.RecordingContent) :
                    RecordingContent.Audio;

                recordingOptions.RecordingFormat = !string.IsNullOrWhiteSpace(recordingRequest.RecordingFormat) ?
                    new RecordingFormat(recordingRequest.RecordingFormat) :
                    RecordingFormat.Wav;

                var startRecordingResponse = await this.callAutomationClient.GetCallRecording()
                    .StartAsync(recordingOptions);
                return startRecordingResponse;
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error occured during the start recording");
                throw;
            }
        }

        public async Task<Response> StopRecording(string recordingId)
        {
            try
            {
                return await this.callAutomationClient.GetCallRecording().StopAsync(recordingId);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error occured during the stop recording");
                throw;
            }
        }

        public async Task PauseRecording(string recordingId)
        {
            try
            {
                await this.callAutomationClient.GetCallRecording().PauseAsync(recordingId);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error occured during the stop recording");
                throw;
            }
        }

        public async Task ResumeRecording(string recordingId)
        {
            try
            {
                await this.callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"Error occured during the stop recording");
                throw;
            }
        }

        public async Task<Dictionary<string,string>> RecordingPath(string recordingId)
        {
            try
            {
                var result = new Dictionary<string, string>();
                string recordingFilePath = $"{Directory.GetCurrentDirectory()}\\{recordingId}";
                result.Add("recordingFilePath", recordingFilePath);
                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
