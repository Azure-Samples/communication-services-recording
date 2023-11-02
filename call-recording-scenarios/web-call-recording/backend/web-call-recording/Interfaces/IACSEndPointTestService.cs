namespace web_call_recording.Interfaces
{
    public interface IACSEndPointTestService
    {
        Task<HttpResponseMessage> TestAcsCreateCallApi(string userIdentity, string apiVersion = "");
        Task<HttpResponseMessage> TestAcsGetRecordingPropertiesApi(string recordingId, string apiVersion = "");
        Task<HttpResponseMessage> TestAcsPauseRecordingApi(string recordingId, string apiVersion = "");
        Task<HttpResponseMessage> TestAcsResumeRecordingApi(string recordingId, string apiVersion = "");
        Task<HttpResponseMessage> TestAcsStartRecordingApi(string serverCallId, string apiVersion = "");
        Task<HttpResponseMessage> TestAcsStopRecordingApi(string recordingId, string apiVersion = "");
    }
}
