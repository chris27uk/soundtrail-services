using FluentAssertions;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.RavenEmbedded.NewCommandId;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenEmbeddedPortResponsesTests
{
    [Fact]
    public async Task Given_A_New_CommandId_When_Acquiring_Then_The_Lock_Is_Acquired()
    {
        using var env = ActiveLookupWorkStoreTestEnvironment.Create();

        var acquired = await env.Store.TryAcquireAsync(
            CommandId.For("mc_track_1"),
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        acquired.Should().BeTrue();
    }
}
