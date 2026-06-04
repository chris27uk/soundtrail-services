using FluentAssertions;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Api.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.Contract;

[Collection(RavenEmbeddedCollection.Name)]
public sealed partial class ActiveLookupWorkStorePortContractTests
{
    public static IEnumerable<object[]> Modes =>
    [
        [ActiveLookupWorkStorePortMode.InProcessFake],
        [ActiveLookupWorkStorePortMode.RavenEmbedded]
    ];

    [Theory]
    [MemberData(nameof(Modes))]
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
