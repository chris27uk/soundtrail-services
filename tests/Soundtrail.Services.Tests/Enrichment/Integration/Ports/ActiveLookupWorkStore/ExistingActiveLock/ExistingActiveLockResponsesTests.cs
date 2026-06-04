using FluentAssertions;
using Soundtrail.Services.Shared;
using Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.NewCommandId;

namespace Soundtrail.Services.Tests.Enrichment.Integration.Ports.ActiveLookupWorkStore.ExistingActiveLock;

public sealed class ExistingActiveLockResponsesTests
{
    [Theory]
    [MemberData(nameof(ActiveLookupWorkStorePortContractModes.All), MemberType = typeof(ActiveLookupWorkStorePortContractModes))]
    public async Task Given_An_Unexpired_Lock_When_Acquiring_Then_The_Lock_Is_Not_Acquired(ActiveLookupWorkStorePortMode mode)
    {
        using var env = ActiveLookupWorkStoreTestEnvironment.Create(mode);
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
