using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class AssessMusicTrackHandlerTests
{
    [Fact]
    public async Task Given_An_Immediate_Search_Candidate_When_Assessed_Then_The_Same_Discovery_Is_Planned_Without_Tracking_Stores()
    {
        var env = AssessMusicTrackHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        env.SeedDiscoveryRequested(searchCriteria);
        env.SeedCandidateIdentified(searchCriteria, MusicCatalogId.From("mc_track_1"));

        await env.Handler.Handle(
            env.ImmediateCommand(searchCriteria, MusicCatalogId.From("mc_track_1"), trustLevel: 1, riskScore: 10),
            CancellationToken.None);

        env.StoredEvents(searchCriteria).Last().Should().BeOfType<DiscoveryPlanned>();
    }

    [Fact]
    public async Task Given_An_Immediate_Suspicious_Search_Candidate_When_Assessed_Then_The_Same_Discovery_Is_Deferred()
    {
        var env = AssessMusicTrackHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        env.SeedDiscoveryRequested(searchCriteria);
        env.SeedCandidateIdentified(searchCriteria, MusicCatalogId.From("mc_track_1"), riskScore: 60);

        await env.Handler.Handle(
            env.ImmediateCommand(searchCriteria, MusicCatalogId.From("mc_track_1"), trustLevel: 1, riskScore: 60),
            CancellationToken.None);

        env.StoredEvents(searchCriteria).Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_An_Immediate_Search_Candidate_For_A_Playable_Local_Track_When_Assessed_Then_The_Same_Discovery_Is_Deferred()
    {
        var env = AssessMusicTrackHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        env.SeedDiscoveryRequested(searchCriteria);
        env.SeedCandidateIdentified(searchCriteria, musicCatalogId);
        env.SeedPlayableTrack(musicCatalogId);

        await env.Handler.Handle(
            env.ImmediateCommand(searchCriteria, musicCatalogId, trustLevel: 1, riskScore: 10),
            CancellationToken.None);

        env.StoredEvents(searchCriteria).Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_A_Deferred_Discovery_Candidate_When_Assessed_From_Backlog_Then_The_Discovery_Is_Planned()
    {
        var env = AssessMusicTrackHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks);
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        env.SeedDiscoveryRequested(searchCriteria);
        env.SeedCandidateIdentified(searchCriteria, musicCatalogId, trustLevel: 2, riskScore: 10);
        env.SeedDiscoveryDeferred(searchCriteria, env.ImmediateCommand(searchCriteria, musicCatalogId).CreatedAt.AddSeconds(-1));

        await env.Handler.Handle(
            env.BacklogCommand(searchCriteria, musicCatalogId),
            CancellationToken.None);

        env.StoredEvents(searchCriteria).Last().Should().BeOfType<DiscoveryPlanned>();
    }
}
