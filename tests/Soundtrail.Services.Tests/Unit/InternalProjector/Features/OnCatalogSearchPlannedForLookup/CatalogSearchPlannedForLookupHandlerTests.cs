using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;
using Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Features.OnCatalogSearchPlannedForLookup;

public sealed class CatalogSearchPlannedForLookupHandlerTests
{
    [Fact]
    public async Task Given_A_Planned_Search_With_No_Local_Track_When_Handled_Then_Metadata_Lookup_Is_Sent_Using_Search_Criteria()
    {
        var env = CatalogSearchPlannedForLookupHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        env.TrackingPointsTo(SyntheticCatalogCandidateId.ForSearch(searchCriteria));
        env.TrackIsMissing();

        await env.Handler.Handle(env.Command(searchCriteria), CancellationToken.None);

        env.Bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<LookupTrackMetadataCommand>()
            .Which.Should().BeEquivalentTo(new
            {
                MusicCatalogId = SyntheticCatalogCandidateId.ForSearch(searchCriteria),
                SearchCriteria = searchCriteria,
                Priority = LookupPriorityBand.High
            });
    }
}
