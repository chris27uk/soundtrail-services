using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Registry;
using KnownTrackRequestedEvent = Soundtrail.Domain.Discovery.Events.KnownTrackRequested;

namespace Soundtrail.Adapters.Discovery;

public sealed class DiscoveryQueryStoredEventTranslator : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<TrackMetadataLookupRequested, TrackMetadataLookupRequestedEventDataRecordDto>(
            nameof(TrackMetadataLookupRequested),
            domainEvent => new TrackMetadataLookupRequestedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.TrustLevel,
                domainEvent.RiskScore,
                domainEvent.RequiredAt,
                domainEvent.CorrelationId.Value),
            dto => new TrackMetadataLookupRequested(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                dto.TrustLevel,
                dto.RiskScore,
                dto.RequiredAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            domainEvent => domainEvent.RequiredAt,
            domainEvent => domainEvent.CorrelationId.Value);

        registry.RegisterStoredEventPair<KnownTrackRequestedEvent, KnownTrackRequestedEventDataRecordDto>(
            nameof(KnownTrackRequestedEvent),
            domainEvent => new KnownTrackRequestedEventDataRecordDto(
                domainEvent.TrackId.Value,
                domainEvent.Playback.ToString(),
                domainEvent.RequestedAt,
                domainEvent.CorrelationId.Value),
            dto => new KnownTrackRequestedEvent(
                TrackId.From(dto.TrackId),
                PlaybackProviderFilter.Parse(dto.Playback),
                dto.RequestedAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            domainEvent => domainEvent.RequestedAt,
            domainEvent => domainEvent.CorrelationId.Value);

        registry.RegisterStoredEventPair<ArtistCatalogLookupRequested, ArtistCatalogLookupRequestedEventDataRecordDto>(
            nameof(ArtistCatalogLookupRequested),
            domainEvent => new ArtistCatalogLookupRequestedEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.RequestedAt,
                domainEvent.CorrelationId.Value),
            dto => new ArtistCatalogLookupRequested(
                ArtistId.From(dto.ArtistId),
                dto.RequestedAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            domainEvent => domainEvent.RequestedAt,
            domainEvent => domainEvent.CorrelationId.Value);

        registry.RegisterStoredEventPair<AlbumCatalogLookupRequested, AlbumCatalogLookupRequestedEventDataRecordDto>(
            nameof(AlbumCatalogLookupRequested),
            domainEvent => new AlbumCatalogLookupRequestedEventDataRecordDto(
                domainEvent.ArtistId?.Value,
                domainEvent.AlbumId.Value,
                domainEvent.RequestedAt,
                domainEvent.CorrelationId.Value),
            dto => new AlbumCatalogLookupRequested(
                dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                AlbumId.From(dto.AlbumId),
                dto.RequestedAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            domainEvent => domainEvent.RequestedAt,
            domainEvent => domainEvent.CorrelationId.Value);

        registry.RegisterStoredEventPair<StreamingLocationsRequired, StreamingLocationsRequiredEventDataRecordDto>(
            nameof(StreamingLocationsRequired),
            domainEvent => new StreamingLocationsRequiredEventDataRecordDto(
                domainEvent.MusicCatalogId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.CorrelationId.Value,
                domainEvent.SourceProvider.Value,
                domainEvent.ObservedAt,
                domainEvent.SearchCriteria.Kind,
                domainEvent.SearchCriteria.UnifiedQuery,
                domainEvent.SearchCriteria.Isrc,
                domainEvent.SearchCriteria.Title,
                domainEvent.SearchCriteria.Artist,
                domainEvent.SearchCriteria.Album,
                domainEvent.Hierarchy?.ArtistId?.Value,
                domainEvent.Hierarchy?.AlbumId?.Value),
            dto => new StreamingLocationsRequired(
                MusicCatalogId.From(dto.MusicCatalogId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                CorrelationId.From(dto.CorrelationId),
                LookupSource.From(dto.SourceProvider),
                dto.ObservedAt,
                dto.SearchKind switch
                {
                    MusicSearchKind.UnifiedSearch => MusicSearchCriteria.ByQuery(
                        dto.Query ?? throw new InvalidOperationException("Stored unified streaming locations event requires a query.")),
                    MusicSearchKind.Isrc => MusicSearchCriteria.ByIsrc(
                        dto.Isrc ?? throw new InvalidOperationException("Stored ISRC streaming locations event requires an ISRC.")),
                    MusicSearchKind.TrackArtistAlbum => MusicSearchCriteria.ByTrackArtistAlbum(
                        dto.Title ?? throw new InvalidOperationException("Stored track/artist/album streaming locations event requires a title."),
                        dto.Artist ?? throw new InvalidOperationException("Stored track/artist/album streaming locations event requires an artist."),
                        dto.Album),
                    _ => throw new InvalidOperationException($"Unsupported music search kind '{dto.SearchKind}'.")
                },
                dto.ArtistId is null && dto.AlbumId is null
                    ? null
                    : new CatalogTrackHierarchy(
                        dto.ArtistId is null ? null : ArtistId.From(dto.ArtistId),
                        dto.AlbumId is null ? null : AlbumId.From(dto.AlbumId))),
            domainEvent => domainEvent.ObservedAt,
            domainEvent => domainEvent.CorrelationId.Value);

        registry.RegisterStoredEventPair<DiscoveryRequested, DiscoveryRequestedEventDataRecordDto>(
            nameof(DiscoveryRequested),
            domainEvent => new DiscoveryRequestedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.SearchCriteria.UnifiedQuery ?? string.Empty,
                domainEvent.TrustLevel,
                domainEvent.RiskScore,
                domainEvent.RequestedAt,
                domainEvent.CorrelationId.Value),
            dto => new DiscoveryRequested(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                dto.TrustLevel,
                dto.RiskScore,
                dto.RequestedAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            domainEvent => domainEvent.RequestedAt,
            domainEvent => domainEvent.CorrelationId.Value);

        registry.RegisterStoredEventPair<DiscoveryPlanned, DiscoveryPlannedEventDataRecordDto>(
            nameof(DiscoveryPlanned),
            domainEvent => new DiscoveryPlannedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.Priority.ToString(),
                domainEvent.WillBeLookedUp,
                domainEvent.EstimatedRetryAfterSeconds,
                domainEvent.EarliestExpectedCompletionAt,
                domainEvent.Reason,
                domainEvent.PlannedAt),
            dto => new DiscoveryPlanned(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.WillBeLookedUp,
                dto.EstimatedRetryAfterSeconds,
                dto.EarliestExpectedCompletionAt,
                dto.Reason,
                dto.PlannedAtUtc),
            domainEvent => domainEvent.PlannedAt);

        registry.RegisterStoredEventPair<DiscoveryDeferred, DiscoveryDeferredEventDataRecordDto>(
            nameof(DiscoveryDeferred),
            domainEvent => new DiscoveryDeferredEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.WillBeLookedUp,
                domainEvent.EstimatedRetryAfterSeconds,
                domainEvent.EarliestExpectedCompletionAt,
                domainEvent.Reason,
                domainEvent.DeferredAt),
            dto => new DiscoveryDeferred(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                dto.WillBeLookedUp,
                dto.EstimatedRetryAfterSeconds,
                dto.EarliestExpectedCompletionAt,
                dto.Reason,
                dto.DeferredAtUtc),
            domainEvent => domainEvent.DeferredAt);

        registry.RegisterStoredEventPair<DiscoveryRejected, DiscoveryRejectedEventDataRecordDto>(
            nameof(DiscoveryRejected),
            domainEvent => new DiscoveryRejectedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.WillBeLookedUp,
                domainEvent.Reason,
                domainEvent.RejectedAt),
            dto => new DiscoveryRejected(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                dto.WillBeLookedUp,
                dto.Reason,
                dto.RejectedAtUtc),
            domainEvent => domainEvent.RejectedAt);

        registry.RegisterStoredEventPair<DiscoveryFailed, DiscoveryFailedEventDataRecordDto>(
            nameof(DiscoveryFailed),
            domainEvent => new DiscoveryFailedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.WillBeLookedUp,
                domainEvent.Reason,
                domainEvent.FailedAt),
            dto => new DiscoveryFailed(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                dto.WillBeLookedUp,
                dto.Reason,
                dto.FailedAtUtc),
            domainEvent => domainEvent.FailedAt);

        registry.RegisterStoredEventPair<DiscoveryStarted, DiscoveryStartedEventDataRecordDto>(
            nameof(DiscoveryStarted),
            domainEvent => new DiscoveryStartedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.Priority.ToString(),
                domainEvent.WillBeLookedUp,
                domainEvent.Reason,
                domainEvent.StartedAt),
            dto => new DiscoveryStarted(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.WillBeLookedUp,
                dto.Reason,
                dto.StartedAtUtc),
            domainEvent => domainEvent.StartedAt);

        registry.RegisterStoredEventPair<DiscoveryCompleted, DiscoveryCompletedEventDataRecordDto>(
            nameof(DiscoveryCompleted),
            domainEvent => new DiscoveryCompletedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.Priority.ToString(),
                domainEvent.WillBeLookedUp,
                domainEvent.Reason,
                domainEvent.CompletedAt),
            dto => new DiscoveryCompleted(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.WillBeLookedUp,
                dto.Reason,
                dto.CompletedAtUtc),
            domainEvent => domainEvent.CompletedAt);

        registry.RegisterStoredEventPair<MusicTrackSearchStarted, MusicTrackSearchStartedEventDataRecordDto>(
            nameof(MusicTrackSearchStarted),
            domainEvent => new MusicTrackSearchStartedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.MusicCatalogId.Value,
                domainEvent.TrustLevel,
                domainEvent.RiskScore,
                domainEvent.StartedAt,
                domainEvent.CorrelationId.Value),
            dto => new MusicTrackSearchStarted(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                MusicCatalogId.From(dto.MusicCatalogId),
                dto.TrustLevel,
                dto.RiskScore,
                dto.StartedAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            domainEvent => domainEvent.StartedAt,
            domainEvent => domainEvent.CorrelationId.Value);
    }
}
