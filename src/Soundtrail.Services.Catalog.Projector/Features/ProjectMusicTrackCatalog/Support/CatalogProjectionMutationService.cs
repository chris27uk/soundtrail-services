using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using System.Text.Json;

namespace Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog.Support;

public sealed class CatalogProjectionMutationService
{
    public CatalogProjectionDocumentIds DescribeRelatedDocuments(
        MusicTrackStoredEventRecordDto storedEvent,
        CatalogTrackRecordDto track)
    {
        return storedEvent.EventType switch
        {
            nameof(ArtistDiscovered) => new CatalogProjectionDocumentIds(
                ArtistId: Deserialize<ArtistDiscoveredEventDataRecordDto>(storedEvent).ArtistId,
                AlbumId: string.IsNullOrWhiteSpace(track.AlbumId) ? null : track.AlbumId),
            nameof(AlbumDiscovered) => new CatalogProjectionDocumentIds(
                ArtistId: null,
                AlbumId: Deserialize<AlbumDiscoveredEventDataRecordDto>(storedEvent).AlbumId),
            nameof(ProviderReferenceDiscovered) => new CatalogProjectionDocumentIds(
                ArtistId: EmptyAsNull(track.ArtistId),
                AlbumId: EmptyAsNull(track.AlbumId)),
            nameof(ProviderReferenceLookupFailed) => new CatalogProjectionDocumentIds(
                ArtistId: EmptyAsNull(track.ArtistId),
                AlbumId: EmptyAsNull(track.AlbumId)),
            nameof(ArtworkDiscovered) => DescribeArtworkRelatedDocuments(storedEvent, track),
            nameof(MetadataCorrected) => DescribeMetadataCorrectionRelatedDocuments(storedEvent, track),
            _ => CatalogProjectionDocumentIds.None
        };
    }

    public void ApplyStoredEvent(
        MusicTrackStoredEventRecordDto storedEvent,
        CatalogProjectionDocuments documents)
    {
        switch (storedEvent.EventType)
        {
            case nameof(TrackDiscovered):
                ApplyMinimalTrackInfo(documents.Track, Deserialize<TrackDiscoveredEventDataRecordDto>(storedEvent));
                break;
            case nameof(ArtistDiscovered):
                ApplyArtistDiscovered(
                    documents.Track,
                    documents.Artist,
                    documents.Album,
                    Deserialize<ArtistDiscoveredEventDataRecordDto>(storedEvent));
                break;
            case nameof(AlbumDiscovered):
                ApplyAlbumDiscovered(
                    documents.Track,
                    documents.Album,
                    Deserialize<AlbumDiscoveredEventDataRecordDto>(storedEvent));
                break;
            case nameof(ProviderReferenceDiscovered):
                ApplyProviderReference(
                    documents.Track,
                    documents.Artist,
                    documents.Album,
                    Deserialize<ProviderReferenceDiscoveredEventDataRecordDto>(storedEvent));
                break;
            case nameof(ProviderReferenceLookupFailed):
                ApplyProviderFailure(
                    documents.Track,
                    documents.Artist,
                    documents.Album,
                    Deserialize<ProviderReferenceLookupFailedEventDataRecordDto>(storedEvent));
                break;
            case nameof(ArtworkDiscovered):
                ApplyArtworkDiscovered(
                    documents.Track,
                    documents.Artist,
                    documents.Album,
                    Deserialize<ArtworkDiscoveredEventDataRecordDto>(storedEvent));
                break;
            case nameof(MetadataCorrected):
                ApplyMetadataCorrected(
                    documents.Track,
                    documents.Artist,
                    documents.Album,
                    Deserialize<MetadataCorrectedEventDataRecordDto>(storedEvent));
                break;
            case nameof(PlaybackReferencesResolutionRequired):
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(storedEvent.EventType), storedEvent.EventType, "Unknown music track event type.");
        }
    }

    private static CatalogProjectionDocumentIds DescribeArtworkRelatedDocuments(
        MusicTrackStoredEventRecordDto storedEvent,
        CatalogTrackRecordDto track)
    {
        var data = Deserialize<ArtworkDiscoveredEventDataRecordDto>(storedEvent);
        var entityKind = Enum.Parse<Domain.Catalog.CatalogEntityKind>(data.EntityKind, ignoreCase: true);

        return entityKind switch
        {
            Domain.Catalog.CatalogEntityKind.Track => CatalogProjectionDocumentIds.None,
            Domain.Catalog.CatalogEntityKind.Artist => new CatalogProjectionDocumentIds(
                ArtistId: EmptyAsNull(data.EntityId) ?? EmptyAsNull(track.ArtistId),
                AlbumId: null),
            Domain.Catalog.CatalogEntityKind.Album => new CatalogProjectionDocumentIds(
                ArtistId: null,
                AlbumId: EmptyAsNull(data.EntityId) ?? EmptyAsNull(track.AlbumId)),
            _ => throw new ArgumentOutOfRangeException(nameof(data.EntityKind), data.EntityKind, null)
        };
    }

    private static CatalogProjectionDocumentIds DescribeMetadataCorrectionRelatedDocuments(
        MusicTrackStoredEventRecordDto storedEvent,
        CatalogTrackRecordDto track)
    {
        var data = Deserialize<MetadataCorrectedEventDataRecordDto>(storedEvent);
        return new CatalogProjectionDocumentIds(
            ArtistId: EmptyAsNull(data.ArtistId) ?? EmptyAsNull(track.ArtistId),
            AlbumId: EmptyAsNull(data.AlbumId) ?? EmptyAsNull(track.AlbumId));
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

    private static void ApplyArtistDiscovered(
        CatalogTrackRecordDto track,
        CatalogArtistRecordDto? artist,
        CatalogAlbumRecordDto? album,
        ArtistDiscoveredEventDataRecordDto data)
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

        if (artist is not null)
        {
            artist.Name = Coalesce(track.ArtistName, artist.Name);
            artist.NormalizedName = Normalize(artist.Name);
            artist.AvailableProviders = MergeProviders(artist.AvailableProviders, track.AvailableProviders);
            artist.TerminallyUnavailableProviders = MergeProviders(artist.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
            artist.ArtworkUrl = CoalesceNullable(track.ArtworkUrl, artist.ArtworkUrl);
            artist.UpdatedAt = data.ObservedAt;
        }

        if (album is not null)
        {
            album.ArtistId = track.ArtistId;
            album.ArtistName = Coalesce(track.ArtistName, album.ArtistName);
            album.AvailableProviders = MergeProviders(album.AvailableProviders, track.AvailableProviders);
            album.TerminallyUnavailableProviders = MergeProviders(album.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
            album.UpdatedAt = data.ObservedAt;
        }
    }

    private static void ApplyAlbumDiscovered(
        CatalogTrackRecordDto track,
        CatalogAlbumRecordDto? album,
        AlbumDiscoveredEventDataRecordDto data)
    {
        if (!string.IsNullOrWhiteSpace(data.AlbumId))
        {
            track.AlbumId = data.AlbumId;
        }

        if (!string.IsNullOrWhiteSpace(data.AlbumTitle))
        {
            track.AlbumName = data.AlbumTitle;
        }

        if (album is not null)
        {
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

    private static void ApplyProviderReference(
        CatalogTrackRecordDto track,
        CatalogArtistRecordDto? artist,
        CatalogAlbumRecordDto? album,
        ProviderReferenceDiscoveredEventDataRecordDto data)
    {
        track.AvailableProviders = AddProvider(track.AvailableProviders, data.Provider);
        track.TerminallyUnavailableProviders = RemoveProvider(track.TerminallyUnavailableProviders, data.Provider);
        track.ProviderReferences = UpsertProviderReference(
            track.ProviderReferences,
            new CatalogProviderReferenceRecordDto
            {
                Provider = data.Provider,
                ProviderEntityType = "track",
                ProviderId = data.ExternalId ?? string.Empty,
                Url = data.Url,
                DiscoveredAt = data.ObservedAt
            });

        if (artist is not null)
        {
            artist.AvailableProviders = AddProvider(artist.AvailableProviders, data.Provider);
            artist.TerminallyUnavailableProviders = RemoveProvider(artist.TerminallyUnavailableProviders, data.Provider);
            artist.UpdatedAt = data.ObservedAt;
        }

        if (album is not null)
        {
            album.AvailableProviders = AddProvider(album.AvailableProviders, data.Provider);
            album.TerminallyUnavailableProviders = RemoveProvider(album.TerminallyUnavailableProviders, data.Provider);
            album.UpdatedAt = data.ObservedAt;
        }
    }

    private static void ApplyProviderFailure(
        CatalogTrackRecordDto track,
        CatalogArtistRecordDto? artist,
        CatalogAlbumRecordDto? album,
        ProviderReferenceLookupFailedEventDataRecordDto data)
    {
        track.TerminallyUnavailableProviders = AddProvider(track.TerminallyUnavailableProviders, data.Provider);
        track.AvailableProviders = RemoveProvider(track.AvailableProviders, data.Provider);
        track.ProviderReferences = RemoveProviderReference(track.ProviderReferences, data.Provider);

        if (artist is not null)
        {
            artist.TerminallyUnavailableProviders = AddProvider(artist.TerminallyUnavailableProviders, data.Provider);
            artist.AvailableProviders = RemoveProvider(artist.AvailableProviders, data.Provider);
            artist.UpdatedAt = data.ObservedAt;
        }

        if (album is not null)
        {
            album.TerminallyUnavailableProviders = AddProvider(album.TerminallyUnavailableProviders, data.Provider);
            album.AvailableProviders = RemoveProvider(album.AvailableProviders, data.Provider);
            album.UpdatedAt = data.ObservedAt;
        }
    }

    private static void ApplyArtworkDiscovered(
        CatalogTrackRecordDto track,
        CatalogArtistRecordDto? artist,
        CatalogAlbumRecordDto? album,
        ArtworkDiscoveredEventDataRecordDto data)
    {
        var entityKind = Enum.Parse<Domain.Catalog.CatalogEntityKind>(data.EntityKind, ignoreCase: true);

        switch (entityKind)
        {
            case Domain.Catalog.CatalogEntityKind.Track:
                track.ArtworkUrl = data.Url;
                break;
            case Domain.Catalog.CatalogEntityKind.Artist:
                if (artist is not null)
                {
                    artist.ArtworkUrl = data.Url;
                    artist.UpdatedAt = data.ObservedAt;
                }

                break;
            case Domain.Catalog.CatalogEntityKind.Album:
                if (album is not null)
                {
                    album.ArtworkUrl = data.Url;
                    album.UpdatedAt = data.ObservedAt;
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(data.EntityKind), data.EntityKind, null);
        }
    }

    private static void ApplyMetadataCorrected(
        CatalogTrackRecordDto track,
        CatalogArtistRecordDto? artist,
        CatalogAlbumRecordDto? album,
        MetadataCorrectedEventDataRecordDto data)
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

        if (artist is not null)
        {
            artist.Name = data.ArtistName;
            artist.NormalizedName = Normalize(data.ArtistName);
            artist.AvailableProviders = MergeProviders(artist.AvailableProviders, track.AvailableProviders);
            artist.TerminallyUnavailableProviders = MergeProviders(artist.TerminallyUnavailableProviders, track.TerminallyUnavailableProviders);
            artist.ArtworkUrl = CoalesceNullable(track.ArtworkUrl, artist.ArtworkUrl);
            artist.UpdatedAt = data.CorrectedAt;
        }

        if (album is not null)
        {
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

    private static T Deserialize<T>(MusicTrackStoredEventRecordDto storedEvent) where T : class =>
        JsonSerializer.Deserialize<T>(storedEvent.Data)
        ?? throw new InvalidOperationException($"Unable to deserialize {storedEvent.EventType}.");

    private static string? EmptyAsNull(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static string[] AddProvider(string[] providers, string provider) =>
        providers.Contains(provider, StringComparer.Ordinal)
            ? providers
            : [.. providers, provider];

    private static string[] RemoveProvider(string[] providers, string provider) =>
        providers.Where(value => !string.Equals(value, provider, StringComparison.Ordinal)).ToArray();

    private static string[] MergeProviders(string[] current, string[] additions) =>
        additions.Aggregate(current, AddProvider);

    private static CatalogProviderReferenceRecordDto[] UpsertProviderReference(
        IEnumerable<CatalogProviderReferenceRecordDto>? current,
        CatalogProviderReferenceRecordDto reference) =>
        (current ?? [])
        .Where(item => !string.Equals(item.Provider, reference.Provider, StringComparison.Ordinal))
        .Append(reference)
        .ToArray();

    private static CatalogProviderReferenceRecordDto[] RemoveProviderReference(
        IEnumerable<CatalogProviderReferenceRecordDto>? current,
        string provider) =>
        (current ?? [])
        .Where(item => !string.Equals(item.Provider, provider, StringComparison.Ordinal))
        .ToArray();

    private static string BuildSearchText(string title, string artistName) =>
        $"{title} {artistName}".Trim().ToLowerInvariant();

    private static string Normalize(string value) => value.Trim().ToLowerInvariant();

    private static string Coalesce(string candidate, string fallback) =>
        string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;

    private static string? CoalesceNullable(string? candidate, string? fallback) =>
        string.IsNullOrWhiteSpace(candidate) ? fallback : candidate;
}

public sealed record CatalogProjectionDocumentIds(
    string? ArtistId,
    string? AlbumId)
{
    public static readonly CatalogProjectionDocumentIds None = new(null, null);
}
