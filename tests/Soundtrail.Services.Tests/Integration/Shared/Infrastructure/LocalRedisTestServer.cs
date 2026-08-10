using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using StackExchange.Redis;
using Xunit.Sdk;

namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

/// <summary>
/// Process-wide Redis for tests.
/// Prefers <c>SOUNDTRAIL_TEST_REDIS</c>, then a running local instance (e.g. AppHost on 6379),
/// otherwise starts Testcontainers (~2–4s cold).
/// </summary>
internal sealed class LocalRedisTestServer
{
    private const ushort RedisPort = 6379;

    private static readonly Regex RedisPortPattern = new(
        @"(?:0\.0\.0\.0|127\.0\.0\.1|\[::\]):(\d+)->6379/tcp",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly SemaphoreSlim Sync = new(1, 1);
    private static LocalRedisTestServer? shared;
    private static IConnectionMultiplexer? sharedMultiplexer;

    private readonly IContainer? container;
    private readonly string connectionString;

    private LocalRedisTestServer(string connectionString, IContainer? container = null)
    {
        this.connectionString = connectionString;
        this.container = container;
    }

    public string ConnectionString => this.connectionString;

    /// <summary>
    /// Returns the shared Redis endpoint, starting or discovering it on first use.
    /// </summary>
    public static async Task<LocalRedisTestServer> StartAsync(CancellationToken cancellationToken = default)
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

                if (TryFromRunningInstance(out var fromLocal))
                {
                    Volatile.Write(ref shared, fromLocal);
                    return fromLocal;
                }

                var container = new ContainerBuilder()
                    .WithImage("redis:7-alpine")
                    .WithPortBinding(RedisPort, true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisPort))
                    .Build();

                await container.StartAsync(cancellationToken);
                var server = new LocalRedisTestServer(
                    $"{container.Hostname}:{container.GetMappedPublicPort(RedisPort)},abortConnect=false",
                    container);
                Volatile.Write(ref shared, server);
                return server;
            }
            catch (Exception exception) when (exception is not SkipException and not OperationCanceledException)
            {
                throw new SkipException($"Redis test container could not be started locally: {exception.Message}");
            }
        }
        finally
        {
            Sync.Release();
        }
    }

    /// <summary>
    /// Shared multiplexer against the shared Redis. Do not dispose — lifetime is the process.
    /// </summary>
    public static async Task<IConnectionMultiplexer> GetSharedMultiplexerAsync(
        CancellationToken cancellationToken = default)
    {
        var existing = Volatile.Read(ref sharedMultiplexer);
        if (existing is not null)
        {
            return existing;
        }

        var server = await StartAsync(cancellationToken);

        await Sync.WaitAsync(cancellationToken);
        try
        {
            if (sharedMultiplexer is not null)
            {
                return sharedMultiplexer;
            }

            var multiplexer = await ConnectionMultiplexer.ConnectAsync(server.ConnectionString);
            Volatile.Write(ref sharedMultiplexer, multiplexer);
            return multiplexer;
        }
        finally
        {
            Sync.Release();
        }
    }

    /// <summary>
    /// No-op: Redis is process-wide and shared across tests.
    /// </summary>
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static bool TryFromEnvironment(out LocalRedisTestServer server)
    {
        var connectionString = Environment.GetEnvironmentVariable("SOUNDTRAIL_TEST_REDIS");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            server = null!;
            return false;
        }

        connectionString = connectionString.Trim();
        if (!connectionString.Contains("abortConnect=", StringComparison.OrdinalIgnoreCase))
        {
            connectionString += ",abortConnect=false";
        }

        server = new LocalRedisTestServer(connectionString);
        return true;
    }

    private static bool TryFromRunningInstance(out LocalRedisTestServer server)
    {
        foreach (var endpoint in DiscoverPublishedEndpoints())
        {
            if (!IsTcpOpen(endpoint.Address, endpoint.Port, TimeSpan.FromMilliseconds(150)))
            {
                continue;
            }

            server = new LocalRedisTestServer($"{endpoint.Address}:{endpoint.Port},abortConnect=false");
            return true;
        }

        server = null!;
        return false;
    }

    private static IEnumerable<(IPAddress Address, int Port)> DiscoverPublishedEndpoints()
    {
        if (IsTcpOpen(IPAddress.Loopback, RedisPort, TimeSpan.FromMilliseconds(50)))
        {
            yield return (IPAddress.Loopback, RedisPort);
        }

        foreach (var port in DiscoverDockerPublishedRedisPorts())
        {
            if (port != RedisPort)
            {
                yield return (IPAddress.Loopback, port);
            }
        }
    }

    private static IEnumerable<int> DiscoverDockerPublishedRedisPorts()
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

        // Prefer Aspire AppHost naming (redis-*) over ephemeral Testcontainers names.
        var ports = new List<(int Priority, int Port)>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.IndexOf("redis", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            var match = RedisPortPattern.Match(line);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var port))
            {
                continue;
            }

            var priority = line.StartsWith("redis-", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
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
}
