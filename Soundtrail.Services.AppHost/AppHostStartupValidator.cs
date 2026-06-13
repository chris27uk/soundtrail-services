using Microsoft.Extensions.Configuration;

namespace Soundtrail.Services.AppHost;

public static class AppHostStartupValidator
{
    public static void Validate(IConfiguration configuration, string contentRootPath)
    {
        ValidateServiceBusConnectionString(configuration);
        ValidateServiceBusEmulator(configuration, contentRootPath);
        ValidateWireMockMappings(configuration, contentRootPath);
    }

    private static void ValidateServiceBusConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("servicebus");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Aspire requires a real ConnectionStrings:servicebus value. Replace the placeholder value in AppHost development settings or user secrets.");
        }

        if (connectionString.Contains("UseDevelopmentEmulator=true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (connectionString.Contains("replace-me", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Aspire requires a real ConnectionStrings:servicebus value. Replace the placeholder value in AppHost development settings or user secrets.");
        }
    }

    private static void ValidateServiceBusEmulator(IConfiguration configuration, string contentRootPath)
    {
        var useServiceBusEmulator = configuration.GetValue("LocalDevelopment:UseServiceBusEmulator", false);
        if (!useServiceBusEmulator)
        {
            return;
        }

        var configPath = Path.Combine(contentRootPath, "servicebus-emulator", "Config.json");
        if (!File.Exists(configPath))
        {
            throw new InvalidOperationException(
                $"LocalDevelopment:UseServiceBusEmulator is enabled, but the Service Bus emulator config file was not found at '{configPath}'.");
        }

        var sqlPassword = configuration["ServiceBusEmulator:SqlPassword"];
        if (string.IsNullOrWhiteSpace(sqlPassword))
        {
            throw new InvalidOperationException(
                "LocalDevelopment:UseServiceBusEmulator is enabled, but ServiceBusEmulator:SqlPassword is missing.");
        }
    }

    private static void ValidateWireMockMappings(IConfiguration configuration, string contentRootPath)
    {
        var useProviderStubs = configuration.GetValue("LocalDevelopment:UseProviderStubs", false);
        if (!useProviderStubs)
        {
            return;
        }

        var mappingsPath = Path.Combine(contentRootPath, "wiremock", "mappings");
        if (!Directory.Exists(mappingsPath))
        {
            throw new InvalidOperationException(
                $"LocalDevelopment:UseProviderStubs is enabled, but the WireMock mappings directory was not found at '{mappingsPath}'.");
        }

        var hasMappings = Directory.EnumerateFiles(mappingsPath, "*.json", SearchOption.TopDirectoryOnly).Any();
        if (!hasMappings)
        {
            throw new InvalidOperationException(
                $"LocalDevelopment:UseProviderStubs is enabled, but no WireMock mapping files were found at '{mappingsPath}'.");
        }
    }
}
