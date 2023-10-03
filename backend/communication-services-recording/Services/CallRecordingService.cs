
namespace communication_services_recording.Services
{
    public class CallRecordingService : ICallRecordingService
    {
        private readonly CallAutomationClient callAutomationClient;

        public CallRecordingService(
            IConfiguration configuration)
        {
            var acsConnectionString = configuration["AcsConnectionString"];
            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);
            this.callAutomationClient = new CallAutomationClient(acsConnectionString);
        }

        public async Task<RecordingStateResult> StartRecording(string serverCallId)
        {
            StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
            recordingOptions.RecordingChannel = RecordingChannel.Unmixed;
            recordingOptions.RecordingContent = RecordingContent.Audio;
            recordingOptions.RecordingFormat = RecordingFormat.Wav;
            var startRecordingResponse = await this.callAutomationClient.GetCallRecording()
                .StartAsync(recordingOptions).ConfigureAwait(false);
            return startRecordingResponse;
        }

        public async Task<RecordingStateResult> StartRecording(string serverCallId, RecordingOptions options)
        {
            StartRecordingOptions recordingOptions = new StartRecordingOptions(new ServerCallLocator(serverCallId));
            recordingOptions.RecordingChannel = options.RecordingChannel;
            recordingOptions.RecordingContent = options.RecordingContent;
            recordingOptions.RecordingFormat = options.RecordingFormat;
            var startRecordingResponse = await this.callAutomationClient.GetCallRecording()
                .StartAsync(recordingOptions).ConfigureAwait(false);
            return startRecordingResponse;
        }

        public Task StopRecording(string recordingId)
        {
            throw new NotImplementedException();
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
