using WireMock.Server;

namespace Soundtrail.Services.Tests.EndToEnd.Shared;

internal sealed class ProviderStubServer : IAsyncDisposable
{
    private readonly WireMockServer server;

    private ProviderStubServer(WireMockServer server)
    {
        this.server = server;
    }

    public string BaseUrl => this.server.Url!;

    public static ProviderStubServer Start()
    {
        var server = WireMockServer.Start();
        WorldTop100ProviderStubs.Configure(server);
        return new ProviderStubServer(server);
    }

    public ValueTask DisposeAsync()
    {
        this.server.Dispose();
        return ValueTask.CompletedTask;
    }
}
