using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class CatalogSearchRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Schedulable_Request_When_Handled_Then_Only_MusicTrackSearchStarted_Is_Appended()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ResolveAs(MusicCatalogId.From("mc_track_1"));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Should()
            .ContainSingle()
            .Which.Should()
            .BeOfType<MusicTrackSearchStarted>()
            .Which.MusicCatalogId.Should()
            .Be(MusicCatalogId.From("mc_track_1"));
    }

    [Fact]
    public async Task Given_A_Query_With_Multiple_High_Scoring_Matches_When_Handled_Then_Each_Match_Is_Recorded()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.92m),
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_2"), 0.90m),
            new MusicCatalogMatch(MusicCatalogId.From("mc_track_3"), 0.79m));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .OfType<MusicTrackSearchStarted>()
            .Select(x => x.MusicCatalogId.Value)
            .Should()
            .BeEquivalentTo("mc_track_1", "mc_track_2");
    }

    [Fact]
    public async Task Given_A_Request_With_No_High_Scoring_Matches_When_Handled_Then_No_Discovery_Event_Is_Appended()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.79m));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 1, riskScore: 10), CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Search("track", "rare unknown song"))
            .Should()
            .BeEmpty();
    }

    [Fact]
    public async Task Given_A_Track_Search_With_A_Local_Release_Date_When_Handled_Then_Release_Date_Matches_Are_Recorded()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.LocalSearch.Seed(new LocalMusicTrackSearchResult(
            MusicCatalogId.From("mc_track_criteria"),
            "Rare Unknown Song",
            "Test Artist",
            "Rare Album",
            null,
            null,
            null,
            IsPlayable: false,
            ReleaseDate: new DateOnly(2004, 6, 7)));
        env.Search.ReturnMatches(
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_1"),
                0.99m,
                new MusicCatalogMatchEvidence(true, null, null, null, null, "mbid-1", new DateOnly(2004, 6, 7))),
            new MusicCatalogMatch(
                MusicCatalogId.From("mc_track_2"),
                1.00m,
                new MusicCatalogMatchEvidence(true, null, null, null, null, "mbid-2", new DateOnly(2005, 6, 7))));

        await env.Handler.Handle(
            env.Request("rare unknown song", trustLevel: 1, riskScore: 10) with
            {
                Criteria = CatalogSearchCriteria.Track(TrackId.From("mc_track_criteria"))
            },
            CancellationToken.None);

        env.DiscoveryRepository
            .GetStoredEvents(CatalogSearchCriteria.Track(TrackId.From("mc_track_criteria")))
            .OfType<MusicTrackSearchStarted>()
            .Select(x => x.MusicCatalogId.Value)
            .Should()
            .Equal("mc_track_1");
    }
}
