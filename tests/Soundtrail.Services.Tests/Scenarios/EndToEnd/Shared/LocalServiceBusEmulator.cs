using Testcontainers.ServiceBus;
using Xunit.Sdk;

namespace Soundtrail.Services.Tests.EndToEnd.Shared;

internal sealed class LocalServiceBusEmulator : IAsyncDisposable
{
    private readonly ServiceBusContainer container;

    private LocalServiceBusEmulator(ServiceBusContainer container)
    {
        this.container = container;
    }

    public string ConnectionString => this.container.GetConnectionString();

    public static async Task<LocalServiceBusEmulator> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var configPath = ResolveConfigPath();
            var container = new ServiceBusBuilder()
                .WithAcceptLicenseAgreement(true)
                .WithConfig(configPath)
                .Build();

            await container.StartAsync(cancellationToken);
            return new LocalServiceBusEmulator(container);
        }
        catch (Exception exception) when (exception is not SkipException)
        {
            throw new SkipException($"Azure Service Bus emulator could not be started locally: {exception.Message}");
        }
    }

    public ValueTask DisposeAsync() => this.container.DisposeAsync();

    private static string ResolveConfigPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "EndToEnd", "Shared", "servicebus-emulator", "Config.json"),
            Path.Combine(AppContext.BaseDirectory, "servicebus-emulator", "Config.json"),
            Path.GetFullPath(
                Path.Combine(
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    "..",
                    "Soundtrail.Services.AppHost",
                    "servicebus-emulator",
                    "Config.json"))
        };

        var configPath = candidates.FirstOrDefault(File.Exists);
        if (configPath is null)
        {
            throw new FileNotFoundException(
                "Azure Service Bus emulator Config.json was not found. Expected it under the test output or AppHost.");
        }

        return configPath;
    }
}
