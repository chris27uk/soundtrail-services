using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Tests.Integration.Ports;

namespace Soundtrail.Services.Tests.Integration.Projector.OnPlaylistTracksDiscovered;

public sealed class RavenStorePlaylistTracksReadModelPortTests : IAsyncDisposable
{
    private static readonly PlaylistId PlaylistId = Soundtrail.Domain.Catalog.Playlists.PlaylistId.FromPlaylistName("projector_playlist_tracks_merge");
    private readonly IDocumentStore documentStore = EmbeddedRavenTestServer.CreateDocumentStore();
    private readonly List<string> cleanupDocumentIds = [];

    [Fact]
    public async Task Given_Subsequent_Empty_Discovery_When_Storing_Then_Existing_Playlist_Tracks_Are_Preserved()
    {
        var discoveredTrackId = TestTrackIds.Create("spotify-track");
        var subject = new RavenStorePlaylistTracksReadModelPort(documentStore);

        await StoreCatalogTrackAsync(discoveredTrackId);

        await subject.StoreAsync(
            new PlaylistTracksDiscovered(
                PlaylistId,
                [discoveredTrackId],
                DateTimeOffset.UtcNow.AddMinutes(-1)),
            CancellationToken.None);

        await subject.StoreAsync(
            new PlaylistTracksDiscovered(
                PlaylistId,
                [],
                DateTimeOffset.UtcNow),
            CancellationToken.None);

        using var session = documentStore.OpenAsyncSession();
        var record = await session.LoadAsync<CatalogPlaylistTracksRecordDto>(
            CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value),
            CancellationToken.None);

        record.Should().NotBeNull();
        record!.TrackIds.Should().ContainSingle().Which.Should().Be(discoveredTrackId.Value);
        record.Tracks.Should().ContainSingle();
        record.Tracks[0].TrackId.Should().Be(discoveredTrackId.Value);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var documentId in cleanupDocumentIds.Distinct(StringComparer.Ordinal))
        {
            await EmbeddedRavenTestServer.DisposeAsync(documentStore, documentId);
        }
    }

    private async Task StoreCatalogTrackAsync(Soundtrail.Domain.Catalog.Tracks.TrackId trackId)
    {
        var documentId = CatalogTrackRecordDto.GetDocumentId(trackId.Value);
        cleanupDocumentIds.Add(documentId);
        cleanupDocumentIds.Add(CatalogPlaylistTracksRecordDto.GetDocumentId(PlaylistId.Value));

        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogTrackRecordDto
            {
                Id = documentId,
                TrackId = trackId.Value,
                MusicCatalogId = trackId.Value,
                Title = "Spotify Track",
                ArtistName = "Spotify Artist",
                AlbumTitle = "Spotify Album",
                DurationMs = 180000,
                Isrc = "GBAYE2400001",
                ReleaseDate = new DateOnly(2024, 1, 1),
                ReleaseType = "studio",
                ArtworkUrl = "https://cdn.soundtrail.test/tracks/spotify-track.jpg",
                UpdatedAt = DateTimeOffset.UtcNow
            },
            documentId);
        await session.SaveChangesAsync();
    }
}
