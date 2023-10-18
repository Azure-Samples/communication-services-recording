

using Azure;

namespace communication_services_recording.Interfaces
{
    public interface ICallRecordingService
    {
        Task<CreateCallResult> CreateCallAsync(string targetId);
        Task<RecordingStateResult> StartRecording(RecordingRequest recordingRequest);
        Task<Response> StopRecording(string recordingId);
        Task PauseRecording(string recordingId);
        Task ResumeRecording(string recordingId);
        Task<Dictionary<string,string>> RecordingPath(string recordingId);
        Task PlayPromptToCustomerAndResumeRecording(string recordingId, string callConnectionId);
    }
}
