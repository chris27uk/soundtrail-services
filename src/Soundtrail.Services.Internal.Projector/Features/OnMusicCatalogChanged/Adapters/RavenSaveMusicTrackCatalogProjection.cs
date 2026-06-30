using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.ProjectionModel;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicCatalogChanged.Adapters;

public sealed class RavenSaveMusicTrackCatalogProjection(
    IAsyncDocumentSession session) : ISaveMusicTrackCatalogProjectionPort
{
    public RavenSaveMusicTrackCatalogProjection(
        IAsyncDocumentSession session,
        object ignored)
        : this(session)
    {
        _ = ignored;
    }

    public async Task SaveAsync(
        ArtistId artistId,
        int version,
        ArtistCatalog aggregate,
        CancellationToken cancellationToken)
    {
        var artist = aggregate.GetArtist();
        if (artist is not null)
        {
            var artistDocumentId = CatalogArtistRecordDto.GetDocumentId(artist.ArtistId.Value);
            var artistDocument = await session.LoadAsync<CatalogArtistRecordDto>(artistDocumentId, cancellationToken)
                ?? new CatalogArtistRecordDto
                {
                    Id = artistDocumentId
                };

            artistDocument.ArtistId = artist.ArtistId.Value;
            artistDocument.Name = artist.Name;
            artistDocument.NormalizedName = NormalizeFreeText(artist.Name);
            artistDocument.SearchText = NormalizeFreeText(artist.Name);
            artistDocument.MusicBrainzArtistId = artist.SourceArtistId;
            artistDocument.AvailableProviders = artist.AvailableProviders.ToArray();
            artistDocument.TerminallyUnavailableProviders = artist.TerminallyUnavailableProviders.ToArray();
            artistDocument.ArtworkUrl = artist.ArtworkUrl;
            artistDocument.UpdatedAt = artist.UpdatedAt;
            await session.StoreAsync(artistDocument, cancellationToken);
        }

        foreach (var album in aggregate.GetAlbums())
        {
            var albumDocumentId = CatalogAlbumRecordDto.GetDocumentId(album.AlbumId.Value);
            var albumDocument = await session.LoadAsync<CatalogAlbumRecordDto>(albumDocumentId, cancellationToken)
                ?? new CatalogAlbumRecordDto
                {
                    Id = albumDocumentId
                };

            albumDocument.ArtistId = album.ArtistId.Value;
            albumDocument.AlbumId = album.AlbumId.Value;
            albumDocument.Name = album.Name;
            albumDocument.NormalizedName = NormalizeFreeText(album.Name);
            albumDocument.ArtistName = album.ArtistName;
            albumDocument.SearchText = NormalizeFreeText($"{album.Name} {album.ArtistName}");
            albumDocument.MusicBrainzReleaseId = album.SourceAlbumId;
            albumDocument.AvailableProviders = album.AvailableProviders.ToArray();
            albumDocument.TerminallyUnavailableProviders = album.TerminallyUnavailableProviders.ToArray();
            albumDocument.ArtworkUrl = album.ArtworkUrl;
            albumDocument.ReleaseDate = album.ReleaseDate;
            albumDocument.UpdatedAt = album.UpdatedAt;
            await session.StoreAsync(albumDocument, cancellationToken);
        }

        foreach (var track in aggregate.GetTracks())
        {
            await SaveCatalogTrackAsync(track, cancellationToken);
            await SaveTrackProjectionAsync(track, version, cancellationToken);
        }

        var checkpointDocumentId = CatalogProjectionCheckpointDocument.GetDocumentId(artistId.Value);
        var checkpoint = await session.LoadAsync<CatalogProjectionCheckpointDocument>(checkpointDocumentId, cancellationToken)
            ?? new CatalogProjectionCheckpointDocument
            {
                Id = checkpointDocumentId
            };
        checkpoint.ArtistId = artistId.Value;
        checkpoint.LastAppliedVersion = version;
        checkpoint.UpdatedAt = aggregate.GetTracks().Select(x => x.UpdatedAt).Append(DateTimeOffset.UtcNow).Max();
        await session.StoreAsync(checkpoint, cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveCatalogTrackAsync(ArtistCatalogTrackView track, CancellationToken cancellationToken)
    {
        var documentId = CatalogTrackRecordDto.GetDocumentId(track.MusicCatalogId.Value);
        var document = await session.LoadAsync<CatalogTrackRecordDto>(documentId, cancellationToken)
            ?? new CatalogTrackRecordDto
            {
                Id = documentId
            };

        document.TrackId = track.MusicCatalogId.Value;
        document.ArtistId = track.ArtistId.Value;
        document.AlbumId = track.AlbumId?.Value ?? string.Empty;
        document.Title = track.Title;
        document.NormalizedTitle = NormalizeFreeText(track.Title);
        document.ArtistName = track.ArtistName;
        document.AlbumName = track.AlbumTitle ?? string.Empty;
        document.SearchText = NormalizeFreeText($"{track.Title} {track.ArtistName}");
        document.MusicBrainzRecordingId = track.Mbid;
        document.Isrc = track.Isrc;
        document.DurationMs = track.DurationMs;
        document.AvailableProviders = track.ProviderReferences.Select(x => x.Provider.Value).Distinct(StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        document.TerminallyUnavailableProviders = track.TerminallyUnavailableProviders.ToArray();
        document.ProviderReferences = track.ProviderReferences.Select(reference => new CatalogProviderReferenceRecordDto
        {
            Provider = reference.Provider.Value,
            ProviderEntityType = "track",
            ProviderId = reference.ExternalId ?? string.Empty,
            Url = reference.Url.ToString(),
            DiscoveredAt = reference.ObservedAt
        }).ToArray();
        document.ArtworkUrl = track.ArtworkUrl;
        document.UpdatedAt = track.UpdatedAt;
        await session.StoreAsync(document, cancellationToken);
    }

    private async Task SaveTrackProjectionAsync(ArtistCatalogTrackView track, int version, CancellationToken cancellationToken)
    {
        var documentId = RavenTrackRecordDto.GetDocumentId(track.MusicCatalogId.Value);
        var document = await session.LoadAsync<RavenTrackRecordDto>(documentId, cancellationToken)
            ?? new RavenTrackRecordDto
            {
                Id = documentId
            };

        document.ArtistId = track.ArtistId.Value;
        document.AlbumId = track.AlbumId?.Value;
        document.Title = track.Title;
        document.Artist = track.ArtistName;
        document.NormalizedArtist = NormalizeFreeText(track.ArtistName);
        document.AlbumTitle = track.AlbumTitle;
        document.NormalizedAlbumTitle = NormalizeFreeText(track.AlbumTitle);
        document.SearchText = RavenTrackRecordDto.BuildSearchText(track.Title, track.ArtistName);
        document.Isrc = track.Isrc;
        document.NormalizedIsrc = NormalizeCompact(track.Isrc);
        document.Mbid = track.Mbid;
        document.NormalizedMbid = NormalizeCompact(track.Mbid);
        document.DurationMs = track.DurationMs;
        document.ReleaseDate = track.ReleaseDate;
        document.ArtworkUrl = track.ArtworkUrl;
        document.ResolvedMetadata = new RavenSongMetadataRecordDto
        {
            Title = track.Title,
            Artist = track.ArtistName,
            Isrc = track.Isrc,
            Mbid = track.Mbid,
            DurationMs = track.DurationMs
        };
        document.AppleReference = ToProviderReference(track, ProviderName.AppleMusic);
        document.YouTubeMusicReference = ToProviderReference(track, ProviderName.YoutubeMusic);
        document.AppleId = track.ProviderReferences.FirstOrDefault(x => x.Provider == ProviderName.AppleMusic)?.ExternalId;
        document.SpotifyId = track.ProviderReferences.FirstOrDefault(x => x.Provider == ProviderName.Spotify)?.ExternalId;
        document.IsPlayable = document.AppleReference is not null
                              || document.YouTubeMusicReference is not null
                              || !string.IsNullOrWhiteSpace(document.SpotifyId);
        document.ProjectionVersion = version;
        await session.StoreAsync(document, cancellationToken);
    }

    private static RavenProviderReferenceRecordDto? ToProviderReference(
        ArtistCatalogTrackView track,
        ProviderName provider)
    {
        var reference = track.ProviderReferences.FirstOrDefault(x => x.Provider == provider);
        return reference is null
            ? null
            : new RavenProviderReferenceRecordDto
            {
                Provider = reference.Provider.Value,
                Url = reference.Url.ToString(),
                ExternalId = reference.ExternalId,
                SourceProvider = reference.SourceProvider.Value
            };
    }

    private static string NormalizeFreeText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = new string(
            value
                .Trim()
                .Select(character => char.IsLetterOrDigit(character) || char.IsWhiteSpace(character)
                    ? char.ToLowerInvariant(character)
                    : ' ')
                .ToArray());

        return string.Join(' ', sanitized.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string NormalizeCompact(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(char.IsLetterOrDigit)
            .Select(char.ToLowerInvariant)
            .ToArray());
    }
}
