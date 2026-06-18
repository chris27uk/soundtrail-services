using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Responses;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Domain.Catalog;

public sealed class CatalogEntityAggregateTests
{
    [Fact]
    public async Task Given_A_MusicBrainz_Response_When_Recorded_Then_Metadata_Discovery_Events_Are_Emitted()
    {
        var store = new MusicTrackStreamStoreFake();
        var aggregate = await CatalogEntityAggregate.LoadAsync(store, MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        aggregate.RecordEnrichmentResponse(MusicBrainzResponse());
        var append = await aggregate.SaveAsync(store, CommandId.For("ResolveCanonicalMetadata:mc_track_1"), CancellationToken.None);

        append.AppendedEvents.Should().ContainItemsAssignableTo<ArtistDiscovered>();
        append.AppendedEvents.Should().ContainItemsAssignableTo<AlbumDiscovered>();
        append.AppendedEvents.Should().ContainItemsAssignableTo<TrackDiscovered>();
    }

    [Fact]
    public async Task Given_A_First_Metadata_Response_With_No_Playback_References_When_Recorded_Then_Playback_Resolution_Is_Required()
    {
        var store = new MusicTrackStreamStoreFake();
        var aggregate = await CatalogEntityAggregate.LoadAsync(store, MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        aggregate.RecordEnrichmentResponse(MusicBrainzResponse());
        var append = await aggregate.SaveAsync(store, CommandId.For("ResolveCanonicalMetadata:mc_track_1"), CancellationToken.None);

        append.AppendedEvents.Should().ContainSingle(x => x is PlaybackReferencesResolutionRequired);
    }

    [Fact]
    public async Task Given_A_Stream_That_Already_Has_Playback_References_When_Recording_Metadata_Response_Then_Playback_Resolution_Is_Not_Required_Again()
    {
        var store = new MusicTrackStreamStoreFake();
        store.Seed(
            MusicCatalogId.From("mc_track_1"),
            new ProviderReferenceDiscovered(
                ProviderName.AppleMusic,
                "apple-1",
                new Uri("https://music.apple.com/track/1"),
                ProviderName.Odesli,
                Clock));
        var aggregate = await CatalogEntityAggregate.LoadAsync(store, MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        aggregate.RecordEnrichmentResponse(MusicBrainzResponse());
        var append = await aggregate.SaveAsync(store, CommandId.For("ResolveCanonicalMetadata:mc_track_1"), CancellationToken.None);

        append.AppendedEvents.Should().NotContain(x => x is PlaybackReferencesResolutionRequired);
    }

    [Fact]
    public async Task Given_A_Metadata_Response_That_Already_Contains_Playback_References_When_Recorded_Then_Playback_Resolution_Is_Not_Required()
    {
        var store = new MusicTrackStreamStoreFake();
        var aggregate = await CatalogEntityAggregate.LoadAsync(store, MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        aggregate.RecordEnrichmentResponse(
            MusicBrainzResponse() with
            {
                References =
                [
                    new ExternalReference(
                        ProviderName.Spotify,
                        new Uri("https://open.spotify.com/track/spotify-1"),
                        "spotify-1")
                ]
            });
        var append = await aggregate.SaveAsync(store, CommandId.For("ResolveCanonicalMetadata:mc_track_1"), CancellationToken.None);

        append.AppendedEvents.Should().ContainSingle(x => x is ProviderReferenceDiscovered);
        append.AppendedEvents.Should().NotContain(x => x is PlaybackReferencesResolutionRequired);
    }

    [Fact]
    public async Task Given_A_Stream_That_Already_Requires_Playback_Resolution_When_Recording_Metadata_Response_Then_Playback_Resolution_Is_Not_Required_Again()
    {
        var store = new MusicTrackStreamStoreFake();
        store.Seed(
            MusicCatalogId.From("mc_track_1"),
            new PlaybackReferencesResolutionRequired(
                MusicCatalogId.From("mc_track_1"),
                LookupPriorityBand.High,
                CorrelationId.From("corr-seeded"),
                ProviderName.MusicBrainz,
                Clock,
                MusicSearchTerm.ByTrackArtistAlbum("Song A", "Artist A", null),
                new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album"))));
        var aggregate = await CatalogEntityAggregate.LoadAsync(store, MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        aggregate.RecordEnrichmentResponse(MusicBrainzResponse());
        var append = await aggregate.SaveAsync(store, CommandId.For("ResolveCanonicalMetadata:mc_track_1"), CancellationToken.None);

        append.AppendedEvents.Should().NotContain(x => x is PlaybackReferencesResolutionRequired);
    }

    [Fact]
    public async Task Given_A_Non_MusicBrainz_Response_With_Failed_Providers_When_Recorded_Then_Only_Provider_Failures_Are_Emitted()
    {
        var store = new MusicTrackStreamStoreFake();
        var aggregate = await CatalogEntityAggregate.LoadAsync(store, MusicCatalogId.From("mc_track_1"), CancellationToken.None);

        aggregate.RecordEnrichmentResponse(
            new EnrichmentResponse(
                CommandId.For("ResolvePlaybackReferences:mc_track_1"),
                MusicCatalogId.From("mc_track_1"),
                ProviderName.Odesli,
                LookupPriorityBand.High,
                Clock,
                null,
                [],
                [new ProviderLookupFailure(ProviderName.Spotify, ProviderName.Odesli)],
                null,
                CorrelationId.From("corr-2")));
        var append = await aggregate.SaveAsync(store, CommandId.For("ResolvePlaybackReferences:mc_track_1"), CancellationToken.None);

        append.AppendedEvents.Should().ContainSingle(x => x is ProviderReferenceLookupFailed);
        append.AppendedEvents.Should().NotContainItemsAssignableTo<TrackDiscovered>();
        append.AppendedEvents.Should().NotContainItemsAssignableTo<ArtistDiscovered>();
        append.AppendedEvents.Should().NotContainItemsAssignableTo<AlbumDiscovered>();
        append.AppendedEvents.Should().NotContainItemsAssignableTo<PlaybackReferencesResolutionRequired>();
    }

    private static EnrichmentResponse MusicBrainzResponse() =>
        new(
            CommandId.For("ResolveCanonicalMetadata:mc_track_1"),
            MusicCatalogId.From("mc_track_1"),
            ProviderName.MusicBrainz,
            LookupPriorityBand.High,
            Clock,
            new SongMetadata("Song A", "Artist A", "isrc-1", "mbid-1", 123000),
            [],
            [],
            new CatalogTrackHierarchy(ArtistId.From("artist_test_artist"), AlbumId.From("album_rare_album")),
            CorrelationId.From("corr-1"));

    private static readonly DateTimeOffset Clock = new(2026, 6, 18, 12, 0, 0, TimeSpan.Zero);
}
