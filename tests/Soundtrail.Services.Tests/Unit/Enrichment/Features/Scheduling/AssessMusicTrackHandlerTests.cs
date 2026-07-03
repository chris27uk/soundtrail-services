using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling;

public sealed class AssessMusicCatalogItemHandlerTests
{
    [Fact]
    public async Task Given_An_Immediate_Search_Candidate_When_Assessed_Then_The_Same_Discovery_Is_Planned_Without_Tracking_Stores()
    {
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
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
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
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
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
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
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
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

    [Fact]
    public async Task Given_A_Search_Assessment_For_A_NonTrack_Item_When_Handled_Then_A_Clear_Error_Is_Raised()
    {
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("the killers", SearchTypesFilter.Artists);

        await env.Handler.Handle(env.ArtistSearchCommand(searchCriteria), CancellationToken.None);

        env.StoredEvents(searchCriteria).Last().Should().BeOfType<ArtistCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_An_Album_Search_Assessment_When_Handled_Then_Album_Catalog_Lookup_Is_Requested()
    {
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
        var searchCriteria = MusicSearchCriteria.ByQuery("hot fuss", SearchTypesFilter.Albums);

        await env.Handler.Handle(env.AlbumSearchCommand(searchCriteria), CancellationToken.None);

        env.StoredEvents(searchCriteria).Last().Should().BeOfType<AlbumCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_An_Artist_Assessment_From_A_Catalog_Item_When_Handled_Then_Artist_Catalog_Lookup_Is_Requested_On_That_Stream()
    {
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
        var knownId = KnownCatalogId.ForArtist(ArtistId.From("artist_parent"));

        await env.Handler.Handle(env.ArtistCatalogItemResourceCommand(), CancellationToken.None);

        env.StoredEvents(knownId).Last().Should().BeOfType<ArtistCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_An_Album_Assessment_From_A_Catalog_Item_When_Handled_Then_Album_Catalog_Lookup_Is_Requested_On_That_Stream()
    {
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
        var knownId = KnownCatalogId.ForArtist(ArtistId.From("artist_parent"));

        await env.Handler.Handle(env.AlbumCatalogItemResourceCommand(), CancellationToken.None);

        env.StoredEvents(knownId).Last().Should().BeOfType<AlbumCatalogLookupRequested>();
    }

    [Fact]
    public async Task Given_A_Track_Assessment_From_A_Catalog_Item_When_Handled_Then_Known_Track_Is_Requested_On_That_Stream()
    {
        var env = AssessMusicCatalogItemHandlerTestEnvironment.Create();
        var knownId = KnownCatalogId.ForTrack(TrackId.From("track_2"));

        await env.Handler.Handle(
            env.TrackCatalogItemResourceCommand(MusicCatalogId.From("mc_track_1")),
            CancellationToken.None);

        env.StoredEvents(knownId).Last().Should().BeOfType<KnownTrackRequestedEvent>();
    }
}
