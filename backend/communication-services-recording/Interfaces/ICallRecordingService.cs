

namespace communication_services_recording.Interfaces
{
    public interface ICallRecordingService
    {
        Task<RecordingStateResult> StartRecording(string serverCallId);
        Task<RecordingStateResult> StartRecording(string serverCallId, RecordingOptions options);
        Task StopRecording(string recordingId);
        Task PauseRecording(string recordingId);
        Task ResumeRecording(string recordingId);
        Task DownloadRecording(string recordingId);
    }
}
