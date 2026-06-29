using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;
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
        env.TrackIsMissing();

        await env.Handler.Handle(
            env.Command(searchCriteria, SyntheticCatalogCandidateId.ForSearch(searchCriteria)),
            CancellationToken.None);

        env.Bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<LookupTrackMetadataCommand>()
            .Which.Should().BeEquivalentTo(new
            {
                MusicCatalogId = SyntheticCatalogCandidateId.ForSearch(searchCriteria),
                SearchCriteria = searchCriteria,
                Priority = LookupPriorityBand.High
            });
    }

    [Fact]
    public async Task Given_A_Planned_Search_With_No_Identified_Candidate_When_Handled_Then_No_Command_Is_Sent()
    {
        var env = CatalogSearchPlannedForLookupHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var command = new CatalogSearchPlannedForLookupCommand(
            searchCriteria,
            [
                new VersionedCatalogSearchDiscoveryEvent(
                    1,
                    new DiscoveryPlanned(
                        searchCriteria,
                        LookupPriorityBand.High,
                        true,
                        30,
                        null,
                        "Planner queued lookup",
                        new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero)))
            ]);

        await env.Handler.Handle(command, CancellationToken.None);

        env.Bus.SentCommands.Should().BeEmpty();
    }
}
