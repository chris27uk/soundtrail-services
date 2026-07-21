using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

public sealed class RavenStoreArtistCatalogReadModelPort(IDocumentStore documentStore) : IStoreArtistCatalogReadModelPort
{
    public async Task StoreAsync(ArtistCatalogReadModel readModel, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        await session.StoreAsync(
            new CatalogArtistRecordDto
            {
                Id = CatalogArtistRecordDto.GetDocumentId(readModel.ArtistId.Value),
                ArtistId = readModel.ArtistId.Value,
                Name = readModel.ArtistName,
                NormalizedName = MusicIdentityText.NormalizeFreeText(readModel.ArtistName),
                SearchText = readModel.ArtistName,
                MusicBrainzArtistId = null,
                AvailableProviders = [],
                TerminallyUnavailableProviders = [],
                ArtworkUrl = readModel.ArtworkUrl,
                UpdatedAt = readModel.UpdatedAt
            },
            cancellationToken);

        await session.StoreAsync(
            new CatalogArtistAlbumsRecordDto
            {
                Id = CatalogArtistAlbumsRecordDto.GetDocumentId(readModel.ArtistId.Value),
                ArtistId = readModel.ArtistId.Value,
                ArtistName = readModel.ArtistName,
                Albums = readModel.Albums
                    .OrderBy(static x => x.ReleaseDate)
                    .ThenBy(static x => x.AlbumTitle, StringComparer.Ordinal)
                    .Select(album => new CatalogArtistAlbumRecordDto
                    {
                        AlbumId = album.AlbumId.StableValue,
                        MusicCatalogId = album.AlbumId.StableValue,
                        AlbumTitle = album.AlbumTitle,
                        ReleaseDate = album.ReleaseDate,
                        ArtworkUrl = album.ArtworkUrl
                    })
                    .ToArray(),
                UpdatedAt = readModel.UpdatedAt
            },
            cancellationToken);

        await session.StoreAsync(
            new CatalogArtistTracksRecordDto
            {
                Id = CatalogArtistTracksRecordDto.GetDocumentId(readModel.ArtistId.Value),
                ArtistId = readModel.ArtistId.Value,
                ArtistName = readModel.ArtistName,
                Tracks = readModel.Tracks
                    .OrderBy(static x => x.Title, StringComparer.Ordinal)
                    .Select(track => new CatalogArtistTrackRecordDto
                    {
                        TrackId = track.TrackId.Value,
                        TrackIdBaseKeyHigh = track.TrackId.BaseKeyHigh,
                        TrackIdBaseKeyLow = track.TrackId.BaseKeyLow,
                        TrackIdSpecificKey = track.TrackId.SpecificKey,
                        MusicCatalogId = track.TrackId.Value,
                        Title = track.Title,
                        ArtistName = track.ArtistName,
                        AlbumTitle = track.AlbumTitle,
                        DurationMs = track.DurationMs,
                        Isrc = track.Isrc,
                        ReleaseDate = track.ReleaseDate,
                        ReleaseType = track.ReleaseType,
                        ArtworkUrl = track.ArtworkUrl
                    })
                    .ToArray(),
                UpdatedAt = readModel.UpdatedAt
            },
            cancellationToken);

        foreach (var album in readModel.Albums)
        {
            await session.StoreAsync(
                new CatalogAlbumRecordDto
                {
                    Id = CatalogAlbumRecordDto.GetDocumentId(album.AlbumId.StableValue),
                    ArtistId = readModel.ArtistId.Value,
                    AlbumId = album.AlbumId.StableValue,
                    Name = album.AlbumTitle,
                    NormalizedName = MusicIdentityText.NormalizeFreeText(album.AlbumTitle),
                    ArtistName = readModel.ArtistName,
                    SearchText = string.Join(" ", new[] { album.AlbumTitle, readModel.ArtistName }.Where(static x => !string.IsNullOrWhiteSpace(x))),
                    MusicBrainzReleaseId = album.SourceAlbumId,
                    AvailableProviders = [],
                    TerminallyUnavailableProviders = [],
                    ArtworkUrl = album.ArtworkUrl,
                    ReleaseDate = album.ReleaseDate,
                    UpdatedAt = readModel.UpdatedAt
                },
                cancellationToken);

            await session.StoreAsync(
                new CatalogAlbumTracksRecordDto
                {
                    Id = CatalogAlbumTracksRecordDto.GetDocumentId(album.AlbumId.StableValue),
                    ArtistId = readModel.ArtistId.Value,
                    AlbumId = album.AlbumId.StableValue,
                    AlbumTitle = album.AlbumTitle,
                    Tracks = readModel.Tracks
                        .Where(track => string.Equals(track.AlbumId, album.AlbumId.StableValue, StringComparison.Ordinal))
                        .OrderBy(static x => x.Title, StringComparer.Ordinal)
                        .Select(track => new CatalogAlbumTrackRecordDto
                        {
                            TrackId = track.TrackId.Value,
                            TrackIdBaseKeyHigh = track.TrackId.BaseKeyHigh,
                            TrackIdBaseKeyLow = track.TrackId.BaseKeyLow,
                            TrackIdSpecificKey = track.TrackId.SpecificKey,
                            MusicCatalogId = track.TrackId.Value,
                            Title = track.Title,
                            ArtistName = track.ArtistName,
                            DurationMs = track.DurationMs,
                            Isrc = track.Isrc,
                            ReleaseDate = track.ReleaseDate,
                            ReleaseType = track.ReleaseType,
                            ArtworkUrl = track.ArtworkUrl
                        })
                        .ToArray(),
                    UpdatedAt = readModel.UpdatedAt
                },
                cancellationToken);
        }

        foreach (var track in readModel.Tracks)
        {
            await session.StoreAsync(
                new CatalogTrackRecordDto
                {
                    Id = CatalogTrackRecordDto.GetDocumentId(track.TrackId.Value),
                    TrackId = track.TrackId.Value,
                    TrackIdBaseKeyHigh = track.TrackId.BaseKeyHigh,
                    TrackIdBaseKeyLow = track.TrackId.BaseKeyLow,
                    TrackIdSpecificKey = track.TrackId.SpecificKey,
                    MusicCatalogId = track.TrackId.Value,
                    Title = track.Title,
                    ArtistName = track.ArtistName,
                    AlbumTitle = track.AlbumTitle,
                    DurationMs = track.DurationMs,
                    Isrc = track.Isrc,
                    ReleaseDate = track.ReleaseDate,
                    ReleaseType = track.ReleaseType,
                    ArtworkUrl = track.ArtworkUrl,
                    UpdatedAt = readModel.UpdatedAt
                },
                cancellationToken);
        }

        await session.SaveChangesAsync(cancellationToken);
    }
}
