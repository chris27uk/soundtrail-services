using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Solitary.Infrastructure.Messaging;

public sealed class DiscoveryEventTranslationRegistrationTests
{
    [Fact]
    public void Work_requested_search_target_round_trips_search_types()
    {
        var target = new EnrichmentTarget.SearchForUnknownCatalogItem(
            new SearchCriteria("Midnight Signals Aurora Lane", SearchType.Track));
        var @event = new WorkRequested(
            target,
            LookupPriorityBand.High,
            100,
            0,
            DateTimeOffset.Parse("2026-07-31T18:20:16Z"),
            CorrelationId.From("corr-search"));

        var dto = TypeTranslationRegistry.Default.ToDto<CatalogDiscoveryWorkRequestedEventDataRecordDto>(@event);
        var roundTripped = TypeTranslationRegistry.Default.ToDomainObject<WorkRequested>(dto);

        dto.SearchTypes.Should().Be((int)SearchType.Track);
        roundTripped.Target.Should().Be(target);
    }

    [Fact]
    public void Work_scheduled_search_target_round_trips_search_types()
    {
        var target = new EnrichmentTarget.SearchForUnknownCatalogItem(
            new SearchCriteria("Midnight Signals Aurora Lane", SearchType.Track));
        var @event = new WorkScheduled(
            target,
            LookupPriorityBand.High,
            DateTimeOffset.Parse("2026-07-31T18:20:16Z"),
            DateTimeOffset.Parse("2026-07-31T18:21:01Z"),
            "Scheduled.",
            DateTimeOffset.Parse("2026-07-31T18:20:16Z"));

        var dto = TypeTranslationRegistry.Default.ToDto<CatalogDiscoveryWorkScheduledEventDataRecordDto>(@event);
        var roundTripped = TypeTranslationRegistry.Default.ToDomainObject<WorkScheduled>(dto);

        dto.SearchTypes.Should().Be((int)SearchType.Track);
        roundTripped.Target.Should().Be(target);
    }
}
