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
    public async Task Given_A_Catalog_Candidate_When_Handled_Then_Work_Is_Appended_And_Assessment_Is_Requested()
    {
        var env = CatalogCandidateIdentifiedHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        await env.Handler.Handle(env.Command(searchCriteria, musicCatalogId), CancellationToken.None);

        env.WorkRepository.GetStoredEvents(musicCatalogId).Should().ContainSingle()
            .Which.Should().BeOfType<CatalogDiscoveryWorkRequested>();
        env.Bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<AssessMusicTrackCommand>();
    }

    [Fact]
    public async Task Given_The_Same_Catalog_Candidate_Replayed_When_Handled_Twice_Then_Work_And_Assessment_Are_Not_Duplicated()
    {
        var env = CatalogCandidateIdentifiedHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var command = env.Command(searchCriteria, musicCatalogId, version: 4);

        await env.Handler.Handle(command, CancellationToken.None);
        await env.Handler.Handle(command, CancellationToken.None);

        env.WorkRepository.GetStoredEvents(musicCatalogId).Should().ContainSingle()
            .Which.Should().BeOfType<CatalogDiscoveryWorkRequested>();
        env.Bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<AssessMusicTrackCommand>();
    }
}
