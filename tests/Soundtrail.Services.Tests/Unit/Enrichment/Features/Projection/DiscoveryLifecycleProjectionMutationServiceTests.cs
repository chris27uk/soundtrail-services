using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.ProjectDiscoveryLifecycle.Support;
using System.Text.Json;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Projection;

public sealed class DiscoveryLifecycleProjectionMutationServiceTests
{
    private readonly DiscoveryLifecycleProjectionMutationService service = new();

    [Fact]
    public void Given_A_Planned_Discovery_Event_When_Applied_Then_The_Status_Is_Planned_With_Planner_Metadata()
    {
        var status = Status();

        service.ApplyStoredEvent(
            StoredEvent(
                "DiscoveryPlanned",
                new DiscoveryPlannedEventDataRecordDto(
                    "search:track:rare unknown song",
                    LookupPriorityBand.High.ToString(),
                    true,
                    30,
                    Clock.AddMinutes(2),
                    "Planner queued lookup",
                    Clock)),
            status);

        status.Status.Should().Be(CatalogSearchLifecycleStatus.Planned.ToString());
        status.Priority.Should().Be(LookupPriorityBand.High.ToString());
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(30);
        status.EarliestExpectedCompletionAt.Should().Be(Clock.AddMinutes(2));
        status.Reason.Should().Be("Planner queued lookup");
        status.UpdatedAt.Should().Be(Clock);
    }

    [Fact]
    public void Given_A_Deferred_Discovery_Event_When_Applied_Then_The_Status_Is_Deferred_With_Retry_Metadata()
    {
        var status = Status();

        service.ApplyStoredEvent(
            StoredEvent(
                "DiscoveryDeferred",
                new DiscoveryDeferredEventDataRecordDto(
                    "search:track:rare unknown song",
                    true,
                    120,
                    Clock.AddMinutes(5),
                    "Budget exhausted",
                    Clock)),
            status);

        status.Status.Should().Be(CatalogSearchLifecycleStatus.Deferred.ToString());
        status.Priority.Should().BeEmpty();
        status.WillBeLookedUp.Should().BeTrue();
        status.EstimatedRetryAfterSeconds.Should().Be(120);
        status.EarliestExpectedCompletionAt.Should().Be(Clock.AddMinutes(5));
        status.Reason.Should().Be("Budget exhausted");
    }

    [Fact]
    public void Given_A_Rejected_Discovery_Event_When_Applied_Then_The_Status_Is_Rejected_And_Retry_Data_Is_Cleared()
    {
        var status = Status();
        status.EstimatedRetryAfterSeconds = 55;
        status.EarliestExpectedCompletionAt = Clock.AddMinutes(1);

        service.ApplyStoredEvent(
            StoredEvent(
                "DiscoveryRejected",
                new DiscoveryRejectedEventDataRecordDto(
                    "search:track:rare unknown song",
                    false,
                    "Ambiguous match set",
                    Clock)),
            status);

        status.Status.Should().Be(CatalogSearchLifecycleStatus.Rejected.ToString());
        status.WillBeLookedUp.Should().BeFalse();
        status.EstimatedRetryAfterSeconds.Should().BeNull();
        status.EarliestExpectedCompletionAt.Should().BeNull();
        status.Reason.Should().Be("Ambiguous match set");
    }

    [Fact]
    public void Given_A_Completed_Discovery_Event_When_Applied_Then_The_Status_Is_Completed_And_Will_Not_Be_Looked_Up()
    {
        var status = Status();

        service.ApplyStoredEvent(
            StoredEvent(
                "DiscoveryCompleted",
                new DiscoveryCompletedEventDataRecordDto(
                    "search:track:rare unknown song",
                    LookupPriorityBand.Low.ToString(),
                    false,
                    "Discovery completed",
                    Clock)),
            status);

        status.Status.Should().Be(CatalogSearchLifecycleStatus.Completed.ToString());
        status.Priority.Should().Be(LookupPriorityBand.Low.ToString());
        status.WillBeLookedUp.Should().BeFalse();
        status.EstimatedRetryAfterSeconds.Should().BeNull();
        status.EarliestExpectedCompletionAt.Should().BeNull();
        status.Reason.Should().Be("Discovery completed");
    }

    private static CatalogSearchStatusRecordDto Status() =>
        new()
        {
            Id = CatalogSearchStatusRecordDto.GetDocumentId("search:track:rare unknown song"),
            Criteria = "search:track:rare unknown song",
            Status = string.Empty,
            Priority = string.Empty
        };

    private static DiscoveryQueryStoredEventRecordDto StoredEvent(string eventType, object data) =>
        new()
        {
            Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId("search:track:rare unknown song", 1),
            Criteria = "search:track:rare unknown song",
            Version = 1,
            EventType = eventType,
            Data = JsonSerializer.Serialize(data),
            OccurredAtUtc = Clock
        };

    private static readonly DateTimeOffset Clock = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
}
