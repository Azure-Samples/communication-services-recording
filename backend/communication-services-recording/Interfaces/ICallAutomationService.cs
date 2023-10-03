namespace communication_services_recording.Interfaces
{
    public interface ICallAutomationService
    {
        Task<CreateCallResult> CreateCall(string callerId, string targetId);
    }
}
