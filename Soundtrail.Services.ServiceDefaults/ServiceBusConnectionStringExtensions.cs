namespace Soundtrail.Services.ServiceDefaults;

public static class ServiceBusConnectionStringExtensions
{
    public static bool IsDevelopmentEmulatorConnectionString(this string? connectionString)
    {
        return !string.IsNullOrWhiteSpace(connectionString)
               && connectionString.Contains("UseDevelopmentEmulator=true", StringComparison.OrdinalIgnoreCase);
    }
}
