using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using StackExchange.Redis;
using Xunit.Sdk;

namespace Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

/// <summary>
/// Process-wide Redis Testcontainer. Tests isolate via unique key prefixes, not per-test containers.
/// </summary>
internal sealed class LocalRedisTestServer
{
    private const ushort RedisPort = 6379;

    private static readonly SemaphoreSlim Sync = new(1, 1);
    private static LocalRedisTestServer? shared;
    private static IConnectionMultiplexer? sharedMultiplexer;

    private readonly IContainer container;

    private LocalRedisTestServer(IContainer container)
    {
        this.container = container;
    }

    public string ConnectionString =>
        $"{container.Hostname}:{container.GetMappedPublicPort(RedisPort)},abortConnect=false";

    /// <summary>
    /// Returns the shared Redis container, starting it on first use.
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
                var container = new ContainerBuilder()
                    .WithImage("redis:7-alpine")
                    .WithPortBinding(RedisPort, true)
                    .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisPort))
                    .Build();

                await container.StartAsync(cancellationToken);
                var server = new LocalRedisTestServer(container);
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
    /// Shared multiplexer against the shared container. Do not dispose — lifetime is the process.
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
}
