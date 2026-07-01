using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Enrichment.Events;
using Soundtrail.Domain.Enrichment.Responses;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Execution.ApplyLookupExecutionReport;

public sealed class MusicCatalogLookupAttemptedHandlerTests
{
    [Fact]
    public async Task Given_A_Lookup_Attempt_When_Handled_Then_Authoritative_Lookup_History_Is_Written()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var attempted = new MusicCatalogLookupAttempted(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            env.Now,
            CorrelationId.From("corr-1"),
            MusicCatalogLookupOutcome.Completed(),
            null);

        await env.Handler.Handle(attempted, CancellationToken.None);

        env.LookupHistoryRepository
            .GetStoredEvents(MusicCatalogLookupId.From(MusicCatalogId.From("mc_track_1")))
            .Should().ContainSingle()
            .Which.Should().BeOfType<MusicCatalogLookupStarted>();
    }

    [Fact]
    public async Task Given_A_Deferred_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Deferred()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();

        await env.HandleAttempted(new MusicCatalogLookupAttempted(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            env.Now,
            CorrelationId.From("corr-1"),
            MusicCatalogLookupOutcome.Deferred(
                "MusicBrainz budget temporarily unavailable",
                env.Now.AddMinutes(1),
                60),
            null));

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryDeferred>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Failed()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();

        await env.HandleAttempted(new MusicCatalogLookupAttempted(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            env.Now,
            CorrelationId.From("corr-1"),
            MusicCatalogLookupOutcome.Failed("Lookup failed"),
            null));

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_When_Handled_Then_All_Tracked_Discoveries_Are_Started_Before_Failing()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        var before = env.StoredEvents("search:track:rare unknown song").Count;

        await env.HandleAttempted(new MusicCatalogLookupAttempted(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            env.Now,
            CorrelationId.From("corr-1"),
            MusicCatalogLookupOutcome.Failed("Lookup failed"),
            null));

        env.StoredEvents("search:track:rare unknown song").Should().HaveCount(before + 2);
        env.StoredEvents("search:track:rare unknown song")[^2].Should().BeOfType<DiscoveryStarted>();
        env.StoredEvents("search:track:rare unknown song")[^1].Should().BeOfType<DiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Completed_Report_When_Handled_Then_Discovery_Is_Marked_As_Started_When_Not_Already_Started()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();

        await env.HandleAttempted(new MusicCatalogLookupAttempted(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            env.Now,
            CorrelationId.From("corr-1"),
            MusicCatalogLookupOutcome.Completed(),
            null,
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks)));

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryStarted>();
    }

    [Fact]
    public async Task Given_A_Failed_Report_With_Search_Criteria_When_Handled_Then_Tracking_Store_Is_Not_Required()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        env.ClearSearchTrackings();

        await env.HandleAttempted(new MusicCatalogLookupAttempted(
            CommandId.For("LookupTrackMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            LookupSource.MusicBrainz,
            LookupPriorityBand.High,
            env.Now,
            CorrelationId.From("corr-1"),
            MusicCatalogLookupOutcome.Failed("Lookup failed"),
            null,
            MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks)));

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Completed_Report_With_Search_Criteria_When_Handled_Then_Tracking_Store_Is_Not_Required()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        env.ClearSearchTrackings();

        await env.HandleAttempted(
            MusicCatalogLookupAttempted.Completed(
                MusicCatalogLookupAttemptedHandlerTestEnvironment.MusicBrainzResponse(),
                MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks)));

        env.StoredEvents("search:track:rare unknown song").Last().Should().BeOfType<DiscoveryCompleted>();
    }

    [Fact]
    public async Task Given_A_Known_Track_Request_When_A_Lookup_Fails_Then_The_Known_Track_Stream_Is_Started_And_Failed()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        env.ClearSearchTrackings();
        await env.SeedKnownTrackRequestAsync();

        await env.HandleAttempted(new MusicCatalogLookupAttempted(
            CommandId.For("LookupStreamingLocations:track_1"),
            MusicCatalogId.From("track_1"),
            LookupSource.Odesli,
            LookupPriorityBand.Low,
            env.Now,
            CorrelationId.From("corr-track"),
            MusicCatalogLookupOutcome.Failed("Lookup failed"),
            null));

        env.StoredEvents("track:track_1")[^2].Should().BeOfType<KnownTrackDiscoveryStarted>();
        env.StoredEvents("track:track_1")[^1].Should().BeOfType<KnownTrackDiscoveryFailed>();
    }

    [Fact]
    public async Task Given_A_Known_Track_Request_When_A_Lookup_Completes_Then_The_Known_Track_Stream_Is_Marked_Completed()
    {
        var env = MusicCatalogLookupAttemptedHandlerTestEnvironment.Create();
        env.ClearSearchTrackings();
        await env.SeedKnownTrackRequestAsync();

        await env.HandleAttempted(
            MusicCatalogLookupAttempted.Completed(new MusicCatalogMetadataFetched(
                CommandId.For("LookupStreamingLocations:track_1"),
                MusicCatalogId.From("track_1"),
                LookupSource.Odesli,
                LookupPriorityBand.Low,
                env.Now,
                null,
                [],
                [],
                new CatalogTrackHierarchy(ArtistId.From("artist_track_1"), null),
                CorrelationId.From("corr-track"))));

        env.StoredEvents("track:track_1").Last().Should().BeOfType<KnownTrackDiscoveryCompleted>();
    }
}
