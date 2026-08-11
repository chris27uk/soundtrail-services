using Soundtrail.Services.Tests.EndToEnd;
using Soundtrail.Services.Tests.Integration.Shared.Infrastructure;

[assembly: AssemblyFixture(typeof(Soundtrail.Services.Tests.TestRunLifetime))]

namespace Soundtrail.Services.Tests;

/// <summary>
/// Assembly-scoped teardown after every test has finished (before MTP's foreground-thread wait).
/// </summary>
public sealed class TestRunLifetime : IAsyncLifetime
{
    public ValueTask InitializeAsync() => ValueTask.CompletedTask;

    public async ValueTask DisposeAsync()
    {
        await EndToEndHostFixture.ShutdownSharedAsync().ConfigureAwait(false);
        await EmbeddedRavenTestServer.ShutdownAsync().ConfigureAwait(false);
    }
}
