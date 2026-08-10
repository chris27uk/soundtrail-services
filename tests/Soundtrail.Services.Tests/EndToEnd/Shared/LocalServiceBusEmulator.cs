using Testcontainers.ServiceBus;
using Xunit.Sdk;

namespace Soundtrail.Services.Tests.EndToEnd.Shared;

/// <summary>
/// Process-wide Azure Service Bus for E2E.
/// Set <c>SOUNDTRAIL_TEST_SERVICEBUS</c> to reuse a local emulator (e.g. AppHost);
/// otherwise starts Testcontainers.
/// </summary>
internal sealed class LocalServiceBusEmulator : IAsyncDisposable
{
    private static readonly SemaphoreSlim Sync = new(1, 1);
    private static LocalServiceBusEmulator? shared;

    private readonly string connectionString;
    private readonly ServiceBusContainer? container;

    private LocalServiceBusEmulator(string connectionString, ServiceBusContainer? container = null)
    {
        this.connectionString = connectionString;
        this.container = container;
    }

    public string ConnectionString => this.connectionString;

    public static async Task<LocalServiceBusEmulator> StartAsync(CancellationToken cancellationToken = default)
    {
        var existing = Volatile.Read(ref shared);
        if (existing is not null)
        {
            return existing;
        }

        await Sync.WaitAsync(cancellationToken);
        try
        {
            if (shared is not null)
            {
                return shared;
            }

            try
            {
                var fromEnvironment = Environment.GetEnvironmentVariable("SOUNDTRAIL_TEST_SERVICEBUS");
                if (!string.IsNullOrWhiteSpace(fromEnvironment))
                {
                    var emulator = new LocalServiceBusEmulator(fromEnvironment.Trim());
                    Volatile.Write(ref shared, emulator);
                    return emulator;
                }

                var configPath = ResolveConfigPath();
                var container = new ServiceBusBuilder()
                    .WithAcceptLicenseAgreement(true)
                    .WithConfig(configPath)
                    .Build();

                await container.StartAsync(cancellationToken);
                var started = new LocalServiceBusEmulator(container.GetConnectionString(), container);
                Volatile.Write(ref shared, started);
                return started;
            }
            catch (Exception exception) when (exception is not SkipException and not OperationCanceledException)
            {
                throw new SkipException($"Azure Service Bus emulator could not be started locally: {exception.Message}");
            }
        }
        finally
        {
            Sync.Release();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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
