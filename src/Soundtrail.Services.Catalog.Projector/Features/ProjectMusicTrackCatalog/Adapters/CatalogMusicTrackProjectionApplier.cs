using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using System.Text.Json;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Adapters;

public sealed class CatalogMusicTrackProjectionApplier
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

        switch (storedEvent.EventType)
        {
            case nameof(TrackDiscovered):
                ApplyMinimalTrackInfo(track, Deserialize<TrackDiscoveredEventDataRecordDto>(storedEvent));
                break;
            case nameof(ArtistDiscovered):
                await ApplyArtistDiscoveredAsync(track, Deserialize<ArtistDiscoveredEventDataRecordDto>(storedEvent), session, cancellationToken);
                break;
            case nameof(AlbumDiscovered):
                await ApplyAlbumDiscoveredAsync(track, Deserialize<AlbumDiscoveredEventDataRecordDto>(storedEvent), session, cancellationToken);
                break;
            case nameof(ProviderReferenceDiscovered):
                await ApplyProviderReferenceAsync(track, Deserialize<ProviderReferenceDiscoveredEventDataRecordDto>(storedEvent), session, cancellationToken);
                break;
            case nameof(ProviderReferenceLookupFailed):
                await ApplyProviderFailureAsync(track, Deserialize<ProviderReferenceLookupFailedEventDataRecordDto>(storedEvent), session, cancellationToken);
                break;
            case nameof(ArtworkDiscovered):
                await ApplyArtworkDiscoveredAsync(track, Deserialize<ArtworkDiscoveredEventDataRecordDto>(storedEvent), session, cancellationToken);
                break;
            case nameof(MetadataCorrected):
                await ApplyMetadataCorrectedAsync(track, Deserialize<MetadataCorrectedEventDataRecordDto>(storedEvent), session, cancellationToken);
                break;
            case nameof(PlaybackReferencesResolutionRequired):
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storedEvent.EventType), storedEvent.EventType, "Unknown music track event type.");
        }

        track.UpdatedAt = storedEvent.OccurredAtUtc;
        checkpoint.LastAppliedVersion = storedEvent.Version;
        checkpoint.UpdatedAt = storedEvent.OccurredAtUtc;

        await session.StoreAsync(checkpoint, cancellationToken);
    }

    private static void ApplyMinimalTrackInfo(
        CatalogTrackRecordDto track,
        TrackDiscoveredEventDataRecordDto data)
    {
        track.Title = data.Title;
        track.NormalizedTitle = Normalize(data.Title);
        track.ArtistName = string.IsNullOrWhiteSpace(track.ArtistName) ? data.Artist : track.ArtistName;
        track.SearchText = BuildSearchText(track.Title, track.ArtistName);
        track.MusicBrainzRecordingId = data.Mbid;
        track.Isrc = data.Isrc;
        track.DurationMs = data.DurationMs;
    }

    private static async Task ApplyArtistDiscoveredAsync(
        CatalogTrackRecordDto track,
        ArtistDiscoveredEventDataRecordDto data,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(data.ArtistId))
        {
            track.ArtistId = data.ArtistId;
        }

        if (!string.IsNullOrWhiteSpace(data.ArtistName))
        {
            track.ArtistName = data.ArtistName;
        }

        track.SearchText = BuildSearchText(track.Title, track.ArtistName);

        if (!string.IsNullOrWhiteSpace(track.ArtistId))
        {
            var artist = await LoadOrCreateArtistAsync(track.ArtistId, session, cancellationToken);
            artist.Name = Coalesce(track.ArtistName, artist.Name);
            artist.NormalizedName = Normalize(artist.Name);
            artist.AvailableProviders = MergeProviders(artist.AvailableProviders, track.AvailableProviders);
            artist.TerminallyUnavailableProviders = MergeProviders(artist.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
            artist.ArtworkUrl = CoalesceNullable(track.ArtworkUrl, artist.ArtworkUrl);
            artist.UpdatedAt = data.ObservedAt;

            if (!string.IsNullOrWhiteSpace(track.AlbumId))
            {
                var album = await LoadOrCreateAlbumAsync(track.AlbumId, session, cancellationToken);
                album.ArtistId = track.ArtistId;
                album.ArtistName = Coalesce(track.ArtistName, album.ArtistName);
                album.AvailableProviders = MergeProviders(album.AvailableProviders, track.AvailableProviders);
                album.TerminallyUnavailableProviders = MergeProviders(album.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
                album.UpdatedAt = data.ObservedAt;
            }
        }
    }

    private static async Task ApplyAlbumDiscoveredAsync(
        CatalogTrackRecordDto track,
        AlbumDiscoveredEventDataRecordDto data,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(data.AlbumId))
        {
            track.AlbumId = data.AlbumId;
        }

        if (!string.IsNullOrWhiteSpace(data.AlbumTitle))
        {
            track.AlbumName = data.AlbumTitle;
        }

        if (!string.IsNullOrWhiteSpace(track.AlbumId))
        {
            var album = await LoadOrCreateAlbumAsync(track.AlbumId, session, cancellationToken);
            album.Name = Coalesce(track.AlbumName, album.Name);
            album.NormalizedName = Normalize(album.Name);
            album.ArtistId = Coalesce(track.ArtistId, album.ArtistId);
            album.ArtistName = Coalesce(track.ArtistName, album.ArtistName);
            album.AvailableProviders = MergeProviders(album.AvailableProviders, track.AvailableProviders);
            album.TerminallyUnavailableProviders = MergeProviders(album.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
            album.ArtworkUrl = CoalesceNullable(track.ArtworkUrl, album.ArtworkUrl);
            album.UpdatedAt = data.ObservedAt;
        }
    }

    private static async Task ApplyProviderReferenceAsync(
        CatalogTrackRecordDto track,
        ProviderReferenceDiscoveredEventDataRecordDto data,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        track.AvailableProviders = AddProvider(track.AvailableProviders, data.Provider);
        track.TerminallyUnavailableProviders = RemoveProvider(track.TerminallyUnavailableProviders, data.Provider);

        if (!string.IsNullOrWhiteSpace(track.ArtistId))
        {
            var artist = await LoadOrCreateArtistAsync(track.ArtistId, session, cancellationToken);
            artist.AvailableProviders = AddProvider(artist.AvailableProviders, data.Provider);
            artist.TerminallyUnavailableProviders = RemoveProvider(artist.TerminallyUnavailableProviders, data.Provider);
            artist.UpdatedAt = data.ObservedAt;
        }

        if (!string.IsNullOrWhiteSpace(track.AlbumId))
        {
            var album = await LoadOrCreateAlbumAsync(track.AlbumId, session, cancellationToken);
            album.AvailableProviders = AddProvider(album.AvailableProviders, data.Provider);
            album.TerminallyUnavailableProviders = RemoveProvider(album.TerminallyUnavailableProviders, data.Provider);
            album.UpdatedAt = data.ObservedAt;
        }
    }

    private static async Task ApplyProviderFailureAsync(
        CatalogTrackRecordDto track,
        ProviderReferenceLookupFailedEventDataRecordDto data,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        track.TerminallyUnavailableProviders = AddProvider(track.TerminallyUnavailableProviders, data.Provider);
        track.AvailableProviders = RemoveProvider(track.AvailableProviders, data.Provider);

        if (!string.IsNullOrWhiteSpace(track.ArtistId))
        {
            var artist = await LoadOrCreateArtistAsync(track.ArtistId, session, cancellationToken);
            artist.TerminallyUnavailableProviders = AddProvider(artist.TerminallyUnavailableProviders, data.Provider);
            artist.AvailableProviders = RemoveProvider(artist.AvailableProviders, data.Provider);
            artist.UpdatedAt = data.ObservedAt;
        }

        if (!string.IsNullOrWhiteSpace(track.AlbumId))
        {
            var album = await LoadOrCreateAlbumAsync(track.AlbumId, session, cancellationToken);
            album.TerminallyUnavailableProviders = AddProvider(album.TerminallyUnavailableProviders, data.Provider);
            album.AvailableProviders = RemoveProvider(album.AvailableProviders, data.Provider);
            album.UpdatedAt = data.ObservedAt;
        }
    }

    private static async Task ApplyArtworkDiscoveredAsync(
        CatalogTrackRecordDto track,
        ArtworkDiscoveredEventDataRecordDto data,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var entityKind = Enum.Parse<Domain.Catalog.CatalogEntityKind>(data.EntityKind, ignoreCase: true);

        switch (entityKind)
        {
            case Domain.Catalog.CatalogEntityKind.Track:
                track.ArtworkUrl = data.Url;
                break;
            case Domain.Catalog.CatalogEntityKind.Artist:
                var artistId = !string.IsNullOrWhiteSpace(data.EntityId) ? data.EntityId : track.ArtistId;
                if (!string.IsNullOrWhiteSpace(artistId))
                {
                    var artist = await LoadOrCreateArtistAsync(artistId, session, cancellationToken);
                    artist.ArtworkUrl = data.Url;
                    artist.UpdatedAt = data.ObservedAt;
                }

                break;
            case Domain.Catalog.CatalogEntityKind.Album:
                var albumId = !string.IsNullOrWhiteSpace(data.EntityId) ? data.EntityId : track.AlbumId;
                if (!string.IsNullOrWhiteSpace(albumId))
                {
                    var album = await LoadOrCreateAlbumAsync(albumId, session, cancellationToken);
                    album.ArtworkUrl = data.Url;
                    album.UpdatedAt = data.ObservedAt;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(data.EntityKind), data.EntityKind, null);
        }
    }

    private static async Task ApplyMetadataCorrectedAsync(
        CatalogTrackRecordDto track,
        MetadataCorrectedEventDataRecordDto data,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        track.Title = data.Title;
        track.NormalizedTitle = Normalize(data.Title);
        track.ArtistName = data.ArtistName;
        track.AlbumName = data.AlbumTitle ?? track.AlbumName;
        track.ArtistId = data.ArtistId ?? track.ArtistId;
        track.AlbumId = data.AlbumId ?? track.AlbumId;
        track.MusicBrainzRecordingId = data.Mbid;
        track.Isrc = data.Isrc;
        track.DurationMs = data.DurationMs;
        track.SearchText = BuildSearchText(track.Title, track.ArtistName);

        if (!string.IsNullOrWhiteSpace(track.ArtistId))
        {
            var artist = await LoadOrCreateArtistAsync(track.ArtistId, session, cancellationToken);
            artist.Name = data.ArtistName;
            artist.NormalizedName = Normalize(data.ArtistName);
            artist.AvailableProviders = MergeProviders(artist.AvailableProviders, track.AvailableProviders);
            artist.TerminallyUnavailableProviders = MergeProviders(artist.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
            artist.ArtworkUrl = CoalesceNullable(track.ArtworkUrl, artist.ArtworkUrl);
            artist.UpdatedAt = data.CorrectedAt;
        }

        if (!string.IsNullOrWhiteSpace(track.AlbumId))
        {
            var album = await LoadOrCreateAlbumAsync(track.AlbumId, session, cancellationToken);
            album.Name = string.IsNullOrWhiteSpace(data.AlbumTitle) ? album.Name : data.AlbumTitle;
            album.NormalizedName = Normalize(album.Name);
            album.ArtistId = Coalesce(track.ArtistId, album.ArtistId);
            album.ArtistName = Coalesce(data.ArtistName, album.ArtistName);
            album.AvailableProviders = MergeProviders(album.AvailableProviders, track.AvailableProviders);
            album.TerminallyUnavailableProviders = MergeProviders(album.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
            album.ArtworkUrl = CoalesceNullable(track.ArtworkUrl, album.ArtworkUrl);
            album.UpdatedAt = data.CorrectedAt;
        }
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
            TerminallyUnavailableProviders = []
        };
        await session.StoreAsync(track, cancellationToken);
        return track;
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

    private static T Deserialize<T>(MusicTrackStoredEventRecordDto storedEvent) where T : class =>
        JsonSerializer.Deserialize<T>(storedEvent.Data)
        ?? throw new InvalidOperationException($"Unable to deserialize {storedEvent.EventType}.");

    private static string[] AddProvider(string[] providers, string provider) =>
        providers.Contains(provider, StringComparer.Ordinal)
            ? providers
            : [.. providers, provider];

    private static string[] RemoveProvider(string[] providers, string provider) =>
        providers.Where(value => !string.Equals(value, provider, StringComparison.Ordinal)).ToArray();

    private static string[] MergeProviders(string[] current, string[] additions) =>
        additions.Aggregate(current, AddProvider);

    private static string BuildSearchText(string title, string artistName) =>
        $"{title} {artistName}".Trim().ToLowerInvariant();

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string Coalesce(string candidate, string fallback) =>
        string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;

    private static string? CoalesceNullable(string? candidate, string? fallback) =>
        string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
}
