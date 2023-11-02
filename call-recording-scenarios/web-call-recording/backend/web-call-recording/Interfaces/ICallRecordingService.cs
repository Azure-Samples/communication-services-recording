namespace web_call_recording.Interfaces
{
    public interface ICallRecordingService
    {
        Task<RecordingStateResult> StartRecording(RecordingRequest recordingRequest);
        Task<Response> StopRecording(string recordingId);
        Task PauseRecording(string recordingId);
        Task ResumeRecording(string recordingId);
        Task<Dictionary<string,string>> RecordingPath(string recordingId);
    }
}
