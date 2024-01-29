namespace web_call_recording.Interfaces
{
    public interface ICallRecordingService
    {
        Task<RecordingStateResult> StartRecording(RecordingRequest recordingRequest);
        Task<Response> StopRecording(string recordingId);
        Task<Response> PauseRecording(string recordingId);
        Task<Response> ResumeRecording(string recordingId);
        Task<Dictionary<string,string>> RecordingPath(string recordingId);
    }
}
