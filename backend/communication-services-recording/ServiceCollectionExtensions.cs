using communication_services_recording.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackendServices(this IServiceCollection services)
    {
        services.AddSingleton<CallAutomationClient>(client =>
        {
            var config = client.GetRequiredService<IConfiguration>();
            var acsConnectionString = config["AcsConnectionString"];

            ArgumentException.ThrowIfNullOrEmpty(acsConnectionString);

         return new CallAutomationClient(acsConnectionString);
        });

        services.AddSingleton<ICallRecordingService, CallRecordingService>();
        services.AddSingleton<IACSEndPointTestService, ACSEndPointTestService>();
        return services;
    }

}
