namespace communication_services_recording.Interfaces
{
    public interface IACSEndPointTestService
    {
        Task<HttpResponseMessage> TestAcsCreateCallApi(string apiVersion = "");
    }
}
