using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
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

    public void MapOntoTrackDocument(CatalogTrackRecordDto document, MusicTrackCatalogProjection projection)
    {
        document.TrackId = projection.Track.TrackId;
        document.ArtistId = projection.Track.ArtistId;
        document.AlbumId = projection.Track.AlbumId;
        document.Title = projection.Track.Title;
        document.NormalizedTitle = projection.Track.NormalizedTitle;
        document.ArtistName = projection.Track.ArtistName;
        document.AlbumName = projection.Track.AlbumName;
        document.SearchText = projection.Track.SearchText;
        document.MusicBrainzRecordingId = projection.Track.MusicBrainzRecordingId;
        document.Isrc = projection.Track.Isrc;
        document.DurationMs = projection.Track.DurationMs;
        document.AvailableProviders = projection.Track.AvailableProviders.ToArray();
        document.TerminallyUnavailableProviders = projection.Track.TerminallyUnavailableProviders.ToArray();
        document.ProviderReferences = projection.Track.ProviderReferences.Select(x => new CatalogProviderReferenceRecordDto
        {
            Provider = x.Provider,
            ProviderEntityType = x.ProviderEntityType,
            ProviderId = x.ProviderId,
            Url = x.Url,
            DiscoveredAt = x.DiscoveredAt
        }).ToArray();
        document.ArtworkUrl = projection.Track.ArtworkUrl;
        document.UpdatedAt = projection.Track.UpdatedAt;
    }

    public void MapOntoArtistDocument(CatalogArtistRecordDto document, CatalogArtistProjection projection)
    {
        document.ArtistId = projection.ArtistId;
        document.Name = projection.Name;
        document.NormalizedName = projection.NormalizedName;
        document.SearchText = MusicIdentityText.NormalizeFreeText(projection.Name);
        document.MusicBrainzArtistId = projection.MusicBrainzArtistId;
        document.AvailableProviders = projection.AvailableProviders.ToArray();
        document.TerminallyUnavailableProviders = projection.TerminallyUnavailableProviders.ToArray();
        document.ArtworkUrl = projection.ArtworkUrl;
        document.UpdatedAt = projection.UpdatedAt;
    }

    public void MapOntoAlbumDocument(CatalogAlbumRecordDto document, CatalogAlbumProjection projection)
    {
        document.AlbumId = projection.AlbumId;
        document.ArtistId = projection.ArtistId;
        document.Name = projection.Name;
        document.NormalizedName = projection.NormalizedName;
        document.ArtistName = projection.ArtistName;
        document.SearchText = MusicIdentityText.NormalizeFreeText($"{projection.Name} {projection.ArtistName}".Trim());
        document.MusicBrainzReleaseId = projection.MusicBrainzReleaseId;
        document.AvailableProviders = projection.AvailableProviders.ToArray();
        document.TerminallyUnavailableProviders = projection.TerminallyUnavailableProviders.ToArray();
        document.ArtworkUrl = projection.ArtworkUrl;
        document.ReleaseDate = projection.ReleaseDate;
        document.UpdatedAt = projection.UpdatedAt;
    }
}
