using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Ports.CatalogSearchStatusStore.PlannedStatus;

public sealed class PlannedStatusResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogSearchStatusStoreContractModes.All), MemberType = typeof(CatalogSearchStatusStoreContractModes))]
    public async Task Given_A_Planned_Status_Update_When_Stored_Then_It_Can_Be_Read_Back(CatalogSearchStatusStoreMode mode)
    {
        using var env = CatalogSearchStatusStoreTestEnvironment.Create(mode);
        var criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

        await env.Port.UpsertAsync(
            new CatalogSearchStatusUpdate(
                criteria,
                CatalogSearchLifecycleStatus.Planned,
                LookupPriorityBand.High,
                WillBeLookedUp: true,
                EstimatedRetryAfterSeconds: 30,
                EarliestExpectedCompletionAt: null,
                Reason: "Planner queued lookup",
                UpdatedAt: new DateTimeOffset(2026, 6, 14, 12, 0, 0, TimeSpan.Zero)),
            CancellationToken.None);

        var actual = await env.LoadAsync(criteria.Value);

        actual.Should().NotBeNull();
        actual!.Criteria.Should().Be(criteria.Value);
        actual.Status.Should().Be("Planned");
        actual.Priority.Should().Be("High");
        actual.WillBeLookedUp.Should().BeTrue();
        actual.EstimatedRetryAfterSeconds.Should().Be(30);
        actual.Reason.Should().Be("Planner queued lookup");
    }
}
