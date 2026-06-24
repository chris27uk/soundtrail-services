using FluentAssertions;
using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackChanged.Adapters;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack;
using Soundtrail.Services.Internal.Projector.Features.OnReplayMusicTrack.Adapters;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

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
            new RavenSaveMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()));
        var musicCatalogId = MusicCatalogId.From("mc_track_1");
        var commandId = CommandId.For("ResolveCanonicalMetadata:mc_track_1");

        var storedEvents = new MusicTrackStoredEventRecordDto[]
        {
            new()
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 1),
                MusicCatalogId = musicCatalogId.Value,
                Version = 1,
                EventType = nameof(TrackDiscovered),
                TrackDiscovered = new TrackDiscoveredEventDataRecordDto(
                    "Song A",
                    "Artist A",
                    123000,
                    "isrc-1",
                    "mbid-1",
                    ProviderName.MusicBrainz.Value,
                    new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
                CausationId = commandId.Value
            },
            new()
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 2),
                MusicCatalogId = musicCatalogId.Value,
                Version = 2,
                EventType = nameof(ProviderReferenceDiscovered),
                ProviderReferenceDiscovered = new ProviderReferenceDiscoveredEventDataRecordDto(
                    ProviderName.AppleMusic.Value,
                    "apple-1",
                    "https://music.apple.com/us/song/song-a?i=apple-1",
                    ProviderName.Odesli.Value,
                    new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero)),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero),
                CausationId = commandId.Value
            }
        };

        await handler.Handle(ToCommand(musicCatalogId, storedEvents), CancellationToken.None);

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
        var commandId = CommandId.For("ResolveCanonicalMetadata:mc_track_1");

        using (var seedSession = raven.Store.OpenAsyncSession())
        {
            await seedSession.StoreAsync(new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 1),
                MusicCatalogId = musicCatalogId.Value,
                Version = 1,
                EventType = nameof(TrackDiscovered),
                TrackDiscovered = new TrackDiscoveredEventDataRecordDto(
                    "Song A",
                    "Artist A",
                    123000,
                    "isrc-1",
                    "mbid-1",
                    ProviderName.MusicBrainz.Value,
                    new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero),
                CausationId = commandId.Value
            });
            await seedSession.StoreAsync(new MusicTrackStoredEventRecordDto
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 2),
                MusicCatalogId = musicCatalogId.Value,
                Version = 2,
                EventType = nameof(ProviderReferenceDiscovered),
                ProviderReferenceDiscovered = new ProviderReferenceDiscoveredEventDataRecordDto(
                    ProviderName.AppleMusic.Value,
                    "apple-1",
                    "https://music.apple.com/us/song/song-a?i=apple-1",
                    ProviderName.Odesli.Value,
                    new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero)),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 1, 0, TimeSpan.Zero),
                CausationId = commandId.Value
            });
            await seedSession.SaveChangesAsync(CancellationToken.None);
        }

        using (var replaySession = raven.Store.OpenAsyncSession())
        {
            var replayHandler = new ReplayMusicTrackHandler(
                new RavenLoadStoredMusicTrackEvents(replaySession),
                new MusicTrackChangedHandler(
                    new RavenLoadMusicTrackProjection(replaySession, new RavenMusicTrackProjectionMapper()),
                    new RavenSaveMusicTrackProjection(replaySession, new RavenMusicTrackProjectionMapper())));

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
        var storedEvent = new MusicTrackStoredEventRecordDto
        {
            Id = MusicTrackStoredEventRecordDto.GetDocumentId("mc_track_1", 1),
            MusicCatalogId = "mc_track_1",
            Version = 1,
            EventType = nameof(TrackDiscovered),
            TrackDiscovered = new TrackDiscoveredEventDataRecordDto(
                "Song A",
                "Artist A",
                123000,
                "isrc-1",
                "mbid-1",
                ProviderName.MusicBrainz.Value,
                new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)),
            OccurredAtUtc = new DateTimeOffset(2026, 6, 6, 12, 0, 0, TimeSpan.Zero)
        };

        using (var session = raven.Store.OpenAsyncSession())
        {
            var handler = new MusicTrackChangedHandler(
                new RavenLoadMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()),
                new RavenSaveMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()));
            await handler.Handle(ToCommand(MusicCatalogId.From("mc_track_1"), [storedEvent]), CancellationToken.None);
            await handler.Handle(ToCommand(MusicCatalogId.From("mc_track_1"), [storedEvent]), CancellationToken.None);
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
            new RavenSaveMusicTrackProjection(session, new RavenMusicTrackProjectionMapper()));
        var musicCatalogId = MusicCatalogId.From("mc_track_1");

        var storedEvents = new MusicTrackStoredEventRecordDto[]
        {
            new()
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 1),
                MusicCatalogId = musicCatalogId.Value,
                Version = 1,
                EventType = nameof(MetadataCorrected),
                MetadataCorrected = new MetadataCorrectedEventDataRecordDto(
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
                    new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 16, 12, 0, 0, TimeSpan.Zero)
            },
            new()
            {
                Id = MusicTrackStoredEventRecordDto.GetDocumentId(musicCatalogId.Value, 2),
                MusicCatalogId = musicCatalogId.Value,
                Version = 2,
                EventType = nameof(ArtworkDiscovered),
                ArtworkDiscovered = new ArtworkDiscoveredEventDataRecordDto(
                    "Track",
                    null,
                    "https://images.example.com/track.png",
                    "worker/musicbrainz",
                    new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero)),
                OccurredAtUtc = new DateTimeOffset(2026, 6, 16, 12, 1, 0, TimeSpan.Zero)
            }
        };

        await handler.Handle(ToCommand(musicCatalogId, storedEvents), CancellationToken.None);

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

    private static MusicTrackChangedCommand ToCommand(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<MusicTrackStoredEventRecordDto> storedEvents) =>
        new(
            musicCatalogId,
            storedEvents
                .OrderBy(x => x.Version)
                .Select(x => new VersionedMusicTrackEvent(x.Version, x.ToDomainEvent()))
                .ToArray());
}
