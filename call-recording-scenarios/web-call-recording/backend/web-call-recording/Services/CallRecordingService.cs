namespace web_call_recording.Services
{
    public class CallRecordingService : ICallRecordingService
    {
        private readonly IConfiguration configuration;
        private readonly CallAutomationClient callAutomationClient;
        private readonly ILogger logger;

        public CallRecordingService(
            ILogger<CallRecordingService> logger,
            CallAutomationClient callAutomationClient,
            IConfiguration configuration)
        {
            this.logger = logger;
            this.callAutomationClient = callAutomationClient;
            this.configuration = configuration;
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

                if (recordingRequest.isByos)
                {
                    string externalStorageUrl = configuration.GetValue<string>("BringYourOwnStorageUrl")!;
                    if (!string.IsNullOrEmpty(externalStorageUrl))
                    {
                        recordingOptions.ExternalStorage = new BlobStorage(new Uri(externalStorageUrl));
                    }
                    else
                    {
                        this.logger.LogError("External storage is not configured.");
                    }
                    
                }

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
