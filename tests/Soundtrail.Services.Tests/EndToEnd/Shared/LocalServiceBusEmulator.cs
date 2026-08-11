using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Testcontainers.ServiceBus;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;
using Xunit.Sdk;

namespace Soundtrail.Services.Tests.EndToEnd.Shared;

/// <summary>
/// Process-wide Azure Service Bus for E2E.
/// Prefers <c>SOUNDTRAIL_TEST_SERVICEBUS</c>, then a running local emulator (AppHost),
/// otherwise starts Testcontainers when allowed (~10–20s cold). Disabled in CI.
/// </summary>
internal sealed class LocalServiceBusEmulator : IAsyncDisposable
{
    private const string DevelopmentEmulatorConnectionStringFormat =
        "Endpoint=sb://127.0.0.1:{0};SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";

    private static readonly Regex AmqpPortPattern = new(
        @"(?:0\.0\.0\.0|127\.0\.0\.1|\[::\]):(\d+)->5672/tcp",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly SemaphoreSlim Sync = new(1, 1);
    private static LocalServiceBusEmulator? shared;

    private readonly string connectionString;

    private LocalServiceBusEmulator(string connectionString)
    {
        this.connectionString = connectionString;
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
                if (TryFromEnvironment(out var fromEnvironment))
                {
                    Volatile.Write(ref shared, fromEnvironment);
                    return fromEnvironment;
                }

                if (TryFromRunningEmulator(out var fromLocal))
                {
                    Volatile.Write(ref shared, fromLocal);
                    return fromLocal;
                }

                if (!TestInfrastructurePolicy.AllowTestcontainers)
                {
                    throw TestInfrastructurePolicy.MissingInfrastructure(
                        "Azure Service Bus emulator",
                        "SOUNDTRAIL_TEST_SERVICEBUS");
                }

                var configPath = ResolveConfigPath();
                var container = new ServiceBusBuilder()
                    .WithAcceptLicenseAgreement(true)
                    .WithConfig(configPath)
                    .Build();

                await container.StartAsync(cancellationToken);
                var started = new LocalServiceBusEmulator(container.GetConnectionString());
                Volatile.Write(ref shared, started);
                return started;
            }
            catch (Exception exception) when (
                exception is not SkipException
                and not OperationCanceledException
                and not InvalidOperationException)
            {
                throw TestInfrastructureException.Unavailable(
                    $"Azure Service Bus emulator could not be started locally: {exception.Message}");
            }
        }
        finally
        {
            Sync.Release();
        }
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static bool TryFromEnvironment(out LocalServiceBusEmulator emulator)
    {
        var connectionString = Environment.GetEnvironmentVariable("SOUNDTRAIL_TEST_SERVICEBUS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            emulator = null!;
            return false;
        }

        emulator = new LocalServiceBusEmulator(connectionString.Trim());
        return true;
    }

    private static bool TryFromRunningEmulator(out LocalServiceBusEmulator emulator)
    {
        foreach (var port in DiscoverPublishedAmqpPorts())
        {
            if (!IsTcpOpen(IPAddress.Loopback, port, TimeSpan.FromMilliseconds(150)))
            {
                continue;
            }

            emulator = new LocalServiceBusEmulator(string.Format(DevelopmentEmulatorConnectionStringFormat, port));
            return true;
        }

        emulator = null!;
        return false;
    }

    private static IEnumerable<int> DiscoverPublishedAmqpPorts()
    {
        if (IsTcpOpen(IPAddress.Loopback, 5672, TimeSpan.FromMilliseconds(50)))
        {
            yield return 5672;
        }

        foreach (var port in DiscoverDockerPublishedAmqpPorts())
        {
            if (port != 5672)
            {
                yield return port;
            }
        }
    }

    private static IEnumerable<int> DiscoverDockerPublishedAmqpPorts()
    {
        string output;
        try
        {
            using var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "ps --format {{.Names}}\t{{.Image}}\t{{.Ports}}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            if (process is null)
            {
                yield break;
            }

            output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(2000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch
                {
                    // best effort
                }

                yield break;
            }
        }
        catch
        {
            yield break;
        }

        // Prefer Aspire AppHost naming (servicebus-*) over ephemeral Testcontainers names.
        var ports = new List<(int Priority, int Port)>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.IndexOf("servicebus-emulator", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            var match = AmqpPortPattern.Match(line);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var port))
            {
                continue;
            }

            var priority = line.StartsWith("servicebus-", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            ports.Add((priority, port));
        }

        foreach (var port in ports.OrderBy(static x => x.Priority).Select(static x => x.Port).Distinct())
        {
            yield return port;
        }
    }

    private static bool IsTcpOpen(IPAddress address, int port, TimeSpan timeout)
    {
        try
        {
            using var client = new TcpClient();
            var connect = client.ConnectAsync(address, port);
            return connect.Wait(timeout) && client.Connected;
        }
        catch
        {
            return false;
        }
    }

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
