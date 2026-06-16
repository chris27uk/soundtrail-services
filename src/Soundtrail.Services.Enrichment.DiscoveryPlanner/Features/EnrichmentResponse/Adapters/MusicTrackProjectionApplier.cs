using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Events;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters.Documents;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.EnrichmentResponse.Adapters;

public sealed class MusicTrackProjectionApplier
{
    public async Task ReplayStreamAsync(
        MusicCatalogId musicCatalogId,
        IReadOnlyList<IMusicTrackEvent> events,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var document = new RavenTrackRecordDto
        {
            Id = RavenTrackRecordDto.GetDocumentId(musicCatalogId.Value)
        };

        for (var index = 0; index < events.Count; index++)
        {
            Apply(document, events[index], index + 1);
        }

        await session.StoreAsync(document, cancellationToken);
    }

    public async Task ApplyStoredEventAsync(
        MusicTrackStoredEventRecordDto storedEvent,
        IAsyncDocumentSession session,
        CancellationToken cancellationToken)
    {
        var documentId = RavenTrackRecordDto.GetDocumentId(storedEvent.MusicCatalogId);
        var document = await session.LoadAsync<RavenTrackRecordDto>(documentId, cancellationToken)
            ?? new RavenTrackRecordDto
            {
                Id = documentId
            };

        if (document.ProjectionVersion >= storedEvent.Version)
        {
            return;
        }

        Apply(document, storedEvent.ToDomainEvent(), storedEvent.Version);
        await session.StoreAsync(document, cancellationToken);
    }

    private static void Apply(
        RavenTrackRecordDto document,
        IMusicTrackEvent @event,
        int version)
    {
        switch (@event)
        {
            case TrackDiscovered minimalTrackInfoDiscovered:
                document.CanonicalMetadata = new RavenSongMetadataRecordDto
                {
                    Title = minimalTrackInfoDiscovered.Title,
                    Artist = minimalTrackInfoDiscovered.Artist,
                    Isrc = minimalTrackInfoDiscovered.Isrc,
                    Mbid = minimalTrackInfoDiscovered.Mbid,
                    DurationMs = minimalTrackInfoDiscovered.DurationMs
                };
                document.Title = minimalTrackInfoDiscovered.Title;
                document.Artist = minimalTrackInfoDiscovered.Artist;
                document.Isrc = minimalTrackInfoDiscovered.Isrc;
                document.Mbid = minimalTrackInfoDiscovered.Mbid;
                document.DurationMs = minimalTrackInfoDiscovered.DurationMs;
                document.SearchText = RavenTrackRecordDto.BuildSearchText(
                    minimalTrackInfoDiscovered.Title,
                    minimalTrackInfoDiscovered.Artist);
                break;
            case ProviderReferenceDiscovered providerPlaybackReferenceResolved:
                ApplyProviderReference(document, providerPlaybackReferenceResolved);
                break;
            case ProviderReferenceLookupFailed:
                break;
            case AlbumDiscovered trackLinkedToAlbum:
                document.AlbumTitle = trackLinkedToAlbum.AlbumTitle;
                document.AlbumId = trackLinkedToAlbum.AlbumId;
                break;
            case PlaybackReferencesResolutionRequired:
                break;
            case ArtistDiscovered artistDiscovered:
                document.ArtistId = artistDiscovered.ArtistId;
                break;
            case ArtworkDiscovered artworkDiscovered:
                if (artworkDiscovered.EntityKind == Domain.Catalog.CatalogEntityKind.Track)
                {
                    document.ArtworkUrl = artworkDiscovered.Url.ToString();
                }
                break;
            case MetadataCorrected metadataCorrected:
                document.CanonicalMetadata = new RavenSongMetadataRecordDto
                {
                    Title = metadataCorrected.Title,
                    Artist = metadataCorrected.ArtistName,
                    Isrc = metadataCorrected.Isrc,
                    Mbid = metadataCorrected.Mbid,
                    DurationMs = metadataCorrected.DurationMs
                };
                document.Title = metadataCorrected.Title;
                document.Artist = metadataCorrected.ArtistName;
                document.ArtistId = metadataCorrected.ArtistId;
                document.AlbumTitle = metadataCorrected.AlbumTitle;
                document.AlbumId = metadataCorrected.AlbumId;
                document.Isrc = metadataCorrected.Isrc;
                document.Mbid = metadataCorrected.Mbid;
                document.DurationMs = metadataCorrected.DurationMs;
                document.SearchText = RavenTrackRecordDto.BuildSearchText(
                    metadataCorrected.Title,
                    metadataCorrected.ArtistName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown music track event.");
        }

        document.IsPlayable =
            document.CanonicalMetadata is not null
            && (document.AppleReference is not null
                || document.YouTubeMusicReference is not null
                || !string.IsNullOrWhiteSpace(document.SpotifyId));
        document.ProjectionVersion = version;
    }

    private static void ApplyProviderReference(
        RavenTrackRecordDto document,
        ProviderReferenceDiscovered providerPlaybackReferenceResolved)
    {
        switch (providerPlaybackReferenceResolved.Provider.Value)
        {
            case "AppleMusic":
                document.AppleReference = new RavenProviderReferenceRecordDto
                {
                    Provider = providerPlaybackReferenceResolved.Provider.Value,
                    Url = providerPlaybackReferenceResolved.Url.ToString(),
                    ExternalId = providerPlaybackReferenceResolved.ExternalId,
                    SourceProvider = providerPlaybackReferenceResolved.SourceProvider.Value
                };
                document.AppleId = providerPlaybackReferenceResolved.ExternalId;
                break;
            case "YoutubeMusic":
                document.YouTubeMusicReference = new RavenProviderReferenceRecordDto
                {
                    Provider = providerPlaybackReferenceResolved.Provider.Value,
                    Url = providerPlaybackReferenceResolved.Url.ToString(),
                    ExternalId = providerPlaybackReferenceResolved.ExternalId,
                    SourceProvider = providerPlaybackReferenceResolved.SourceProvider.Value
                };
                break;
            case "Spotify":
                document.SpotifyId = providerPlaybackReferenceResolved.ExternalId;
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(providerPlaybackReferenceResolved.Provider),
                    providerPlaybackReferenceResolved.Provider,
                    null);
        }
    }
}
