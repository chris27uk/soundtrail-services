namespace Soundtrail.Services.ServiceDefaults;

/// <summary>
/// Resolves the OpenTelemetry <c>service.version</c> from environment.
/// Assembly metadata stays pinned at 1.0.0 for incremental builds; deploy sets <c>OTEL_SERVICE_VERSION</c>.
/// </summary>
public static class ServiceVersion
{
    public const string EnvironmentVariableName = "OTEL_SERVICE_VERSION";
    public const string Fallback = "1.0.0";

    public static string Resolve(Func<string, string?>? getEnvironmentVariable = null)
    {
        var read = getEnvironmentVariable ?? Environment.GetEnvironmentVariable;
        var fromEnv = read(EnvironmentVariableName);
        return string.IsNullOrWhiteSpace(fromEnv) ? Fallback : fromEnv.Trim();
    }
}
