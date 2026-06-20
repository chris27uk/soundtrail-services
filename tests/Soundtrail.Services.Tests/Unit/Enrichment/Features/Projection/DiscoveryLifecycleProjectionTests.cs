using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Projection;

public sealed class DiscoveryLifecycleProjectionTests
{
    [Fact]
    public void Given_A_Planned_Discovery_Event_When_Applied_Then_The_Status_Is_Planned_With_Planner_Metadata()
    {
        var projection = new DiscoveryLifecycleProjection(Criteria);

        projection.Apply(
            new DiscoveryPlanned(
                Criteria,
                LookupPriorityBand.High,
                true,
                30,
                Clock.AddMinutes(2),
                "Planner queued lookup",
                Clock),
            1);

        projection.Status.Should().Be(CatalogSearchLifecycleStatus.Planned.ToString());
        projection.Priority.Should().Be(LookupPriorityBand.High.ToString());
        projection.WillBeLookedUp.Should().BeTrue();
        projection.EstimatedRetryAfterSeconds.Should().Be(30);
        projection.EarliestExpectedCompletionAt.Should().Be(Clock.AddMinutes(2));
        projection.Reason.Should().Be("Planner queued lookup");
        projection.UpdatedAt.Should().Be(Clock);
    }

    [Fact]
    public void Given_A_Deferred_Discovery_Event_When_Applied_Then_The_Status_Is_Deferred_With_Retry_Metadata()
    {
        var projection = new DiscoveryLifecycleProjection(Criteria);

        projection.Apply(
            new DiscoveryDeferred(
                Criteria,
                true,
                120,
                Clock.AddMinutes(5),
                "Budget exhausted",
                Clock),
            1);

        projection.Status.Should().Be(CatalogSearchLifecycleStatus.Deferred.ToString());
        projection.Priority.Should().BeEmpty();
        projection.WillBeLookedUp.Should().BeTrue();
        projection.EstimatedRetryAfterSeconds.Should().Be(120);
        projection.EarliestExpectedCompletionAt.Should().Be(Clock.AddMinutes(5));
        projection.Reason.Should().Be("Budget exhausted");
    }

    [Fact]
    public void Given_A_Rejected_Discovery_Event_When_Applied_Then_The_Status_Is_Rejected_And_Retry_Data_Is_Cleared()
    {
        var projection = DiscoveryLifecycleProjection.Load(
            new DiscoveryLifecycleProjectionSnapshot(
                Criteria,
                CatalogSearchLifecycleStatus.Planned.ToString(),
                LookupPriorityBand.High.ToString(),
                true,
                55,
                Clock.AddMinutes(1),
                "Existing status",
                Clock,
                1));

        projection.Apply(
            new DiscoveryRejected(
                Criteria,
                false,
                "Ambiguous match set",
                Clock),
            2);

        projection.Status.Should().Be(CatalogSearchLifecycleStatus.Rejected.ToString());
        projection.WillBeLookedUp.Should().BeFalse();
        projection.EstimatedRetryAfterSeconds.Should().BeNull();
        projection.EarliestExpectedCompletionAt.Should().BeNull();
        projection.Reason.Should().Be("Ambiguous match set");
    }

    [Fact]
    public void Given_A_Completed_Discovery_Event_When_Applied_Then_The_Status_Is_Completed_And_Will_Not_Be_Looked_Up()
    {
        var projection = new DiscoveryLifecycleProjection(Criteria);

        projection.Apply(
            new DiscoveryCompleted(
                Criteria,
                LookupPriorityBand.Low,
                false,
                "Discovery completed",
                Clock),
            1);

        projection.Status.Should().Be(CatalogSearchLifecycleStatus.Completed.ToString());
        projection.Priority.Should().Be(LookupPriorityBand.Low.ToString());
        projection.WillBeLookedUp.Should().BeFalse();
        projection.EstimatedRetryAfterSeconds.Should().BeNull();
        projection.EarliestExpectedCompletionAt.Should().BeNull();
        projection.Reason.Should().Be("Discovery completed");
    }

    private static readonly CatalogSearchCriteria Criteria = CatalogSearchCriteria.Search("track", "rare unknown song");

    private static readonly DateTimeOffset Clock = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
}
