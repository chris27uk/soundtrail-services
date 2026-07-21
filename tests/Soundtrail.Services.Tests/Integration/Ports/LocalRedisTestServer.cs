using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Xunit.Sdk;

namespace Soundtrail.Services.Tests.Integration.Ports;

internal sealed class LocalRedisTestServer : IAsyncDisposable
{
    private const ushort RedisPort = 6379;
    private readonly IContainer container;

    private LocalRedisTestServer(IContainer container)
    {
        this.container = container;
    }

    public string ConnectionString =>
        $"{container.Hostname}:{container.GetMappedPublicPort(RedisPort)},abortConnect=false";

    public static async Task<LocalRedisTestServer> StartAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var container = new ContainerBuilder()
                .WithImage("redis:7-alpine")
                .WithPortBinding(RedisPort, true)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RedisPort))
                .Build();

            await container.StartAsync(cancellationToken);
            return new LocalRedisTestServer(container);
        }
        catch (Exception exception) when (exception is not SkipException)
        {
            throw new SkipException($"Redis test container could not be started locally: {exception.Message}");
        }
    }

    public ValueTask DisposeAsync() => container.DisposeAsync();
}
