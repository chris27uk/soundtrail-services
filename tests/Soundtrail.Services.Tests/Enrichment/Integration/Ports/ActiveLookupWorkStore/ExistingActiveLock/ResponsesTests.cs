using FluentAssertions;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.RavenEmbedded.ExistingActiveLock;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenEmbeddedPortResponsesTests
{
    [Fact]
    public async Task Given_An_Unexpired_Lock_When_Acquiring_Then_The_Lock_Is_Not_Acquired()
    {
        using var env = ActiveLookupWorkStoreTestEnvironment.Create();
        await env.Store.TryAcquireAsync(
            CommandId.For("mc_track_1"),
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        var acquired = await env.Store.TryAcquireAsync(
            CommandId.For("mc_track_1"),
            DateTimeOffset.UtcNow.AddMinutes(10),
            CancellationToken.None);

        acquired.Should().BeFalse();
    }
}
