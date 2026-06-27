using Soundtrail.Contracts.Common;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

public sealed class RavenMusicTrackCatalogProjectionMapper
{
    public MusicTrackCatalogProjection ToDomain(
        MusicCatalogId musicCatalogId,
        CatalogTrackRecordDto? track,
        CatalogArtistRecordDto? artist,
        CatalogAlbumRecordDto? album,
        CatalogProjectionCheckpointDocument? checkpoint)
    {
        var snapshot = new MusicTrackCatalogProjectionSnapshot(
            musicCatalogId,
            new CatalogTrackProjection(
                track?.TrackId ?? musicCatalogId.Value,
                track?.ArtistId ?? string.Empty,
                track?.AlbumId ?? string.Empty,
                track?.Title ?? string.Empty,
                track?.NormalizedTitle ?? string.Empty,
                track?.ArtistName ?? string.Empty,
                track?.AlbumName ?? string.Empty,
                track?.SearchText ?? string.Empty,
                track?.MusicBrainzRecordingId,
                track?.Isrc,
                track?.DurationMs,
                track?.AvailableProviders ?? [],
                track?.TerminallyUnavailableProviders ?? [],
                (track?.ProviderReferences ?? []).Select(x => new CatalogProviderReferenceProjection(
                    x.Provider,
                    x.ProviderEntityType,
                    x.ProviderId,
                    x.Url,
                    x.DiscoveredAt)).ToArray(),
                track?.ArtworkUrl,
                track?.UpdatedAt ?? default),
            artist is null
                ? null
                : new CatalogArtistProjection(
                    artist.ArtistId,
                    artist.Name,
                    artist.NormalizedName,
                    artist.MusicBrainzArtistId,
                    artist.AvailableProviders,
                    artist.TerminallyUnavailableProviders,
                    artist.ArtworkUrl,
                    artist.UpdatedAt),
            album is null
                ? null
                : new CatalogAlbumProjection(
                    album.AlbumId,
                    album.ArtistId,
                    album.Name,
                    album.NormalizedName,
                    album.ArtistName,
                    album.MusicBrainzReleaseId,
                    album.AvailableProviders,
                    album.TerminallyUnavailableProviders,
                    album.ArtworkUrl,
                    album.ReleaseDate,
                    album.UpdatedAt),
            checkpoint?.LastAppliedVersion ?? 0);

        return MusicTrackCatalogProjection.Load(snapshot);
    }
}
