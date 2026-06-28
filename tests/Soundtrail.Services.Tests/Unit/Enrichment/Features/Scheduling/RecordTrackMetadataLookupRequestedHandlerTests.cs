using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class RecordTrackMetadataLookupRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Metadata_Record_Command_When_Handled_Then_A_Track_Metadata_Lookup_Requested_Event_Is_Appended()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        var command = new RecordTrackMetadataLookupRequestedCommand(
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks),
            1,
            10,
            new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero),
            CorrelationId.From("corr-1"));

        await env.RecordTrackMetadataLookupRequestedHandler.Handle(command, CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<TrackMetadataLookupRequested>();
    }
}
