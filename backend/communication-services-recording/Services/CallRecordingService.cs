
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

        public async Task StopRecording(string recordingId)
        {
            await this.callAutomationClient.GetCallRecording().StopAsync(recordingId);
        }

        public async Task PauseRecording(string recordingId)
        {
            var pauseRecording = await callAutomationClient.GetCallRecording().PauseAsync(recordingId);
        }

        public async Task ResumeRecording(string recordingId)
        {
            var resumeRecording = await callAutomationClient.GetCallRecording().ResumeAsync(recordingId);
        }

        public async Task DownloadRecording(string recordingId)
        {
            var recordingDownloadUri = new Uri("contentLocation"); // Get contentLocation attribute of the recordingChunk
            var response = await callAutomationClient.GetCallRecording().DownloadToAsync(recordingDownloadUri, "fileName");
        }
    }
}
