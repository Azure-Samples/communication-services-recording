

namespace communication_services_recording.Interfaces
{
    public interface ICallRecordingService
    {
        Task<CreateCallResult> CreateCallAsync(string targetId);
        Task<RecordingStateResult> StartRecording(RecordingRequest recordingRequest);
        Task StopRecording(string recordingId);
        Task PauseRecording(string recordingId);
        Task ResumeRecording(string recordingId);
    }
}
