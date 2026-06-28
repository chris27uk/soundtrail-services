using FluentAssertions;
using Soundtrail.Adapters.Registry;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Commands;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Adapters.ProjectionDocuments;
using Soundtrail.Services.Tests.Support;

namespace Soundtrail.Services.Tests.Integration.Enrichment.Features.ProjectionReplay;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class MusicTrackProjectionReplayResponsesTests
{

    [Fact]
    public async Task Given_Replayable_Stored_Events_When_Projecting_Then_The_Track_Becomes_Playable()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        using var session = raven.Store.OpenAsyncSession();
        var handler = new MusicTrackChangedHandler(
            new RavenLoadMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()),
            new RavenSaveMusicTrackProjection(session, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default));
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        await handler.Handle(
            new MusicTrackChangedCommand(
                musicCatalogId,
                [
                    new VersionedMusicTrackEvent(
                        1,
                        new TrackDiscovered(
                            "Song A",
                            "Artist A",
                            123000,
                            "isrc-1",
                            "mbid-1",
                            LookupSource.MusicBrainz,
                            new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero))),
                    new VersionedMusicTrackEvent(
                        2,
                        new ProviderReferenceDiscovered(
                            ProviderName.AppleMusic,
                            "apple-1",
                            new Uri("https://music.apple.com/us/song/song-a?i=apple-1"),
                            LookupSource.Odesli,
                            new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero)))
                ]),
            CancellationToken.None);

        await session.SaveChangesAsync(CancellationToken.None);

        using var verificationSession = raven.Store.OpenAsyncSession();
        var projection = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            CancellationToken.None);

        projection.Should().NotBeNull();
        projection!.Title.Should().Be("Song A");
        projection.Artist.Should().Be("Artist A");
        projection.AppleId.Should().Be("apple-1");
        projection.IsPlayable.Should().BeTrue();
        projection.ProjectionVersion.Should().Be(2);
    }

    [Fact]
    public async Task Given_Persisted_MusicTrack_Stored_Events_When_Replaying_Then_The_Track_Projection_Is_Rebuilt_From_The_Event_Stream()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        using (var seedSession = raven.Store.OpenAsyncSession())
        {
            var repository = TestEventStreamRepositories.CreateMusicTrack(seedSession);
            await repository.AppendAsync(
                LoadedEventStream<MusicCatalogId, IMusicTrackEvent>.Empty(musicCatalogId),
                [
                    new TrackDiscovered(
                        "Song A",
                        "Artist A",
                        123000,
                        "isrc-1",
                        "mbid-1",
                        LookupSource.MusicBrainz,
                        new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)),
                    new ProviderReferenceDiscovered(
                        ProviderName.AppleMusic,
                        "apple-1",
                        new Uri("https://music.apple.com/us/song/song-a?i=apple-1"),
                        LookupSource.Odesli,
                        new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero))
                ],
                OperationId.From("ResolveMusicMetadata:mc_track_1"),
                CancellationToken.None);
            await seedSession.SaveChangesAsync(CancellationToken.None);
        }

        using (var replaySession = raven.Store.OpenAsyncSession())
        {
            var replayHandler = new ReplayMusicTrackHandler(
                new RavenLoadStoredMusicTrackEvents(replaySession, TypeTranslationRegistry.Default),
                new MusicTrackChangedHandler(
                    new RavenLoadMusicTrackProjection(replaySession, new RavenMusicTrackProjectionMapper()),
                    new RavenSaveMusicTrackProjection(replaySession, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default)));

            await replayHandler.Handle(
                new ReplayMusicTrackCommand(musicCatalogId),
                CancellationToken.None);
        }

        using var verificationSession = raven.Store.OpenAsyncSession();
        var projection = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            CancellationToken.None);

        projection.Should().NotBeNull();
        projection!.Title.Should().Be("Song A");
        projection.Artist.Should().Be("Artist A");
        projection.AppleId.Should().Be("apple-1");
        projection.IsPlayable.Should().BeTrue();
        projection.ProjectionVersion.Should().Be(2);
    }

    [Fact]
    public async Task Given_An_Already_Applied_Event_Version_When_Projecting_Then_The_Duplicate_Is_Ignored()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        var storedEvent = new VersionedMusicTrackEvent(
            1,
            new TrackDiscovered(
                "Song A",
                "Artist A",
                123000,
                "isrc-1",
                "mbid-1",
                LookupSource.MusicBrainz,
                new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)));

        using (var session = raven.Store.OpenAsyncSession())
        {
            var handler = new MusicTrackChangedHandler(
                new RavenLoadMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()),
                new RavenSaveMusicTrackProjection(session, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default));
            await handler.Handle(new MusicTrackChangedCommand(MusicCatalogId.From("mc_track_1"), [storedEvent]), CancellationToken.None);
            await handler.Handle(new MusicTrackChangedCommand(MusicCatalogId.From("mc_track_1"), [storedEvent]), CancellationToken.None);
            await session.SaveChangesAsync(CancellationToken.None);
        }

        using var verificationSession = raven.Store.OpenAsyncSession();
        var projection = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId("mc_track_1"),
            CancellationToken.None);

        projection.Should().NotBeNull();
        projection!.ProjectionVersion.Should().Be(1);
    }

    [Fact]
    public async Task Given_Artwork_And_Metadata_Correction_Events_When_Projecting_Then_The_Track_Projection_Is_Updated()
    {
        using var raven = RavenEmbeddedTestDatabase.Create();
        using var session = raven.Store.OpenAsyncSession();
        var handler = new MusicTrackChangedHandler(
            new RavenLoadMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()),
            new RavenSaveMusicTrackProjection(session, Soundtrail.Adapters.Registry.TypeTranslationRegistry.Default));
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        await handler.Handle(
            new MusicTrackChangedCommand(
                musicCatalogId,
                [
                    new VersionedMusicTrackEvent(
                        1,
                        new MetadataCorrected(
                            "Song A (Remastered)",
                            "Artist A",
                            "artist_a",
                            "mb-artist-a",
                            "Album A",
                            "album_a",
                            "mb-release-a",
                            new DateOnly(2004, 6, 7),
                            123000,
                            "isrc-1",
                            "mbid-1",
                            "admin/repair",
                            new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero))),
                    new VersionedMusicTrackEvent(
                        2,
                        new ArtworkDiscovered(
                            CatalogEntityKind.Track,
                            null,
                            new Uri("https://images.example.com/track.png"),
                            "worker/musicbrainz",
                            new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero)))
                ]),
            CancellationToken.None);

        await session.SaveChangesAsync(CancellationToken.None);

        using var verificationSession = raven.Store.OpenAsyncSession();
        var projection = await verificationSession.LoadAsync<RavenTrackRecordDto>(
            RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value),
            CancellationToken.None);

        projection.Should().NotBeNull();
        projection!.Title.Should().Be("Song A (Remastered)");
        projection.Artist.Should().Be("Artist A");
        projection.ArtistId.Should().Be("artist_a");
        projection.AlbumId.Should().Be("album_a");
        projection.AlbumTitle.Should().Be("Album A");
        projection.ReleaseDate.Should().Be(new DateOnly(2004, 6, 7));
        projection.ArtworkUrl.Should().Be("https://images.example.com/track.png");
        projection.ProjectionVersion.Should().Be(2);
    }

}
