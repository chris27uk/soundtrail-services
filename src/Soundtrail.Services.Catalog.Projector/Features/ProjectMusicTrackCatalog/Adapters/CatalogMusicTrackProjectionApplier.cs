using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Support;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;

public sealed class CatalogMusicTrackProjectionApplier(
    CatalogProjectionMutationService mutationService)
{
    public async Task ApplyStoredEventAsync(
        MusicTrackStoredEventRecordDto storedEvent,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var checkpoint = await session.LoadAsync<CatalogProjectionCheckpointDocument>(
                             CatalogProjectionCheckpointDocument.GetDocumentId(storedEvent.MusicCatalogId),
                             cancellationToken)
                         ?? new CatalogProjectionCheckpointDocument
                         {
                             Id = CatalogProjectionCheckpointDocument.GetDocumentId(storedEvent.MusicCatalogId),
                             MusicCatalogId = storedEvent.MusicCatalogId
                         };

        if (checkpoint.LastAppliedVersion >= storedEvent.Version)
        {
            return;
        }

        var track = await LoadOrCreateTrackAsync(storedEvent.MusicCatalogId, session, cancellationToken);
        var related = mutationService.DescribeRelatedDocuments(storedEvent, track);
        var artist = await LoadArtistIfRequiredAsync(related.ArtistId, session, cancellationToken);
        var album = await LoadAlbumIfRequiredAsync(related.AlbumId, session, cancellationToken);

        mutationService.ApplyStoredEvent(storedEvent, new CatalogProjectionDocuments(track, artist, album));

        track.UpdatedAt = storedEvent.OccurredAtUtc;
        checkpoint.LastAppliedVersion = storedEvent.Version;
        checkpoint.UpdatedAt = storedEvent.OccurredAtUtc;

        await session.StoreAsync(checkpoint, cancellationToken);
    }

    private static async Task<CatalogTrackRecordDto> LoadOrCreateTrackAsync(
        string musicCatalogId,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var documentId = CatalogTrackRecordDto.GetDocumentId(musicCatalogId);
        var track = await session.LoadAsync<CatalogTrackRecordDto>(documentId, cancellationToken);
        if (track is not null)
        {
            track.AvailableProviders ??= [];
            track.TerminallyUnavailableProviders ??= [];
            track.ProviderReferences ??= [];
            return track;
        }

        track = new CatalogTrackRecordDto
        {
            Id = documentId,
            TrackId = musicCatalogId,
            ArtistId = string.Empty,
            AlbumId = string.Empty,
            Title = string.Empty,
            NormalizedTitle = string.Empty,
            ArtistName = string.Empty,
            AlbumName = string.Empty,
            SearchText = string.Empty,
            AvailableProviders = [],
            TerminallyUnavailableProviders = [],
            ProviderReferences = []
        };
        await session.StoreAsync(track, cancellationToken);
        return track;
    }

    private static async Task<CatalogArtistRecordDto?> LoadArtistIfRequiredAsync(
        string? artistId,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(artistId))
        {
            return null;
        }

        return await LoadOrCreateArtistAsync(artistId, session, cancellationToken);
    }

    private static async Task<CatalogArtistRecordDto> LoadOrCreateArtistAsync(
        string artistId,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var documentId = CatalogArtistRecordDto.GetDocumentId(artistId);
        var artist = await session.LoadAsync<CatalogArtistRecordDto>(documentId, cancellationToken);
        if (artist is not null)
        {
            artist.AvailableProviders ??= [];
            artist.TerminallyUnavailableProviders ??= [];
            return artist;
        }

        artist = new CatalogArtistRecordDto
        {
            Id = documentId,
            ArtistId = artistId,
            Name = string.Empty,
            NormalizedName = string.Empty,
            AvailableProviders = [],
            TerminallyUnavailableProviders = []
        };
        await session.StoreAsync(artist, cancellationToken);
        return artist;
    }

    private static async Task<CatalogAlbumRecordDto?> LoadAlbumIfRequiredAsync(
        string? albumId,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(albumId))
        {
            return null;
        }

        return await LoadOrCreateAlbumAsync(albumId, session, cancellationToken);
    }

    private static async Task<CatalogAlbumRecordDto> LoadOrCreateAlbumAsync(
        string albumId,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var documentId = CatalogAlbumRecordDto.GetDocumentId(albumId);
        var album = await session.LoadAsync<CatalogAlbumRecordDto>(documentId, cancellationToken);
        if (album is not null)
        {
            album.AvailableProviders ??= [];
            album.TerminallyUnavailableProviders ??= [];
            return album;
        }

        album = new CatalogAlbumRecordDto
        {
            Id = documentId,
            AlbumId = albumId,
            ArtistId = string.Empty,
            Name = string.Empty,
            NormalizedName = string.Empty,
            ArtistName = string.Empty,
            AvailableProviders = [],
            TerminallyUnavailableProviders = []
        };
        await session.StoreAsync(album, cancellationToken);
        return album;
    }
}
