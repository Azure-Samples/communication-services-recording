using communication_services_recording.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendServices(this IServiceCollection services)
    {
        services.AddSingleton<ICallRecordingService, CallRecordingService>();
        return services;
    }

}
