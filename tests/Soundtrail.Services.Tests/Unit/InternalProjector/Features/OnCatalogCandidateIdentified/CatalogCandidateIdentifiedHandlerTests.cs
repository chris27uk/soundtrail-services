using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Features.OnCatalogCandidateIdentified;

public sealed class CatalogCandidateIdentifiedHandlerTests
{
    [Fact]
    public async Task Given_A_Catalog_Candidate_When_Dispatched_For_Assessment_Then_Assessment_Is_Requested()
    {
        var env = CatalogCandidateIdentifiedHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        await env.DispatchAssessmentHandler.Handle(env.Command(searchCriteria, musicCatalogId, version: 4), CancellationToken.None);

        env.Bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<AssessMusicTrackCommand>();
    }
}
