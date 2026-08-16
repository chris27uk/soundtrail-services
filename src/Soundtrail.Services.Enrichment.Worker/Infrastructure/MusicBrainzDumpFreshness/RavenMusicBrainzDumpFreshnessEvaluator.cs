using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicBrainzDumpFreshness;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicBrainzDumpFreshness;

public sealed class RavenMusicBrainzDumpFreshnessEvaluator(
    IDocumentStore documentStore,
    IOptions<MusicBrainzDumpFreshnessOptions> options) : IMusicBrainzDumpFreshnessEvaluator
{
    public async Task<MusicBrainzDumpFreshnessDecision> EvaluateArtistAlbumsAsync(
        ArtistId artistId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();
        var artist = await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);
        var albums = await session.LoadAsync<CatalogArtistAlbumsRecordDto>(
            CatalogArtistAlbumsRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);

        return MusicBrainzDumpFreshnessPolicy.EvaluateArtistAlbums(
            MapArtist(artist),
            MapAlbums(albums),
            utcNow,
            options.Value.FreshWithin);
    }

    public async Task<MusicBrainzDumpFreshnessDecision> EvaluateArtistTracksAsync(
        ArtistId artistId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        using var session = documentStore.OpenAsyncSession();
        var artist = await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);
        var tracks = await session.Query<CatalogTrackRecordDto>()
            .Where(track => track.ArtistId == artistId.Value)
            .ToListAsync(cancellationToken);

        return MusicBrainzDumpFreshnessPolicy.EvaluateArtistTracks(
            MapArtist(artist),
            tracks.Select(MapTrack).ToArray(),
            utcNow,
            options.Value.FreshWithin);
    }

    public async Task<MusicBrainzDumpFreshnessDecision> EvaluateAlbumTracksAsync(
        AlbumId albumId,
        DateTimeOffset utcNow,
        CancellationToken cancellationToken = default)
    {
        var artistId = ArtistId.From(albumId.ArtistId);
        using var session = documentStore.OpenAsyncSession();
        var artist = await session.LoadAsync<CatalogArtistRecordDto>(
            CatalogArtistRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);
        var albums = await session.LoadAsync<CatalogArtistAlbumsRecordDto>(
            CatalogArtistAlbumsRecordDto.GetDocumentId(artistId.Value),
            cancellationToken);
        var tracks = await session.Query<CatalogTrackRecordDto>()
            .Where(track => track.ArtistId == artistId.Value)
            .ToListAsync(cancellationToken);

        return MusicBrainzDumpFreshnessPolicy.EvaluateAlbumTracks(
            MapArtist(artist),
            MapAlbums(albums),
            albumId,
            tracks.Select(MapTrack).ToArray(),
            utcNow,
            options.Value.FreshWithin);
    }

    private static DumpCatalogArtistSnapshot? MapArtist(CatalogArtistRecordDto? artist) =>
        artist is null
            ? null
            : new DumpCatalogArtistSnapshot(artist.ArtistId, artist.MusicBrainzArtistId, artist.UpdatedAt);

    private static DumpCatalogAlbumsSnapshot? MapAlbums(CatalogArtistAlbumsRecordDto? albums) =>
        albums is null
            ? null
            : new DumpCatalogAlbumsSnapshot(
                albums.ArtistId,
                albums.UpdatedAt,
                albums.Albums
                    .Select(static album => new DumpCatalogAlbumSnapshot(
                        album.AlbumId,
                        album.AlbumTitle,
                        album.ReleaseDate,
                        album.ArtworkUrl))
                    .ToArray());

    private static DumpCatalogTrackSnapshot MapTrack(CatalogTrackRecordDto track) =>
        new(
            track.TrackId,
            track.ArtistId,
            track.Title,
            track.ArtistName,
            track.AlbumTitle,
            track.AlbumId,
            track.DurationMs,
            track.Isrc,
            track.ReleaseDate,
            track.ReleaseType,
            track.ArtworkUrl,
            track.UpdatedAt);
}
