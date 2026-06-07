using FluentAssertions;
using Soundtrail.Contracts;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.NewCommandId;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class NewCommandIdResponsesTests
{
    [Theory]
    [MemberData(nameof(ActiveLookupWorkStorePortContractModes.All), MemberType = typeof(ActiveLookupWorkStorePortContractModes))]
    public async Task Given_A_New_CommandId_When_Acquiring_Then_The_Lock_Is_Acquired(ActiveLookupWorkStorePortMode mode)
    {
        using var env = ActiveLookupWorkStoreTestEnvironment.Create(mode);

        var acquired = await env.Store.TryAcquireAsync(
            CommandId.For("mc_track_1"),
            DateTimeOffset.UtcNow.AddMinutes(5),
            CancellationToken.None);

        acquired.Should().BeTrue();
    }
}