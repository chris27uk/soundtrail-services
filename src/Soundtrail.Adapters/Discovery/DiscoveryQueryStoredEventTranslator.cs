using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
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

        registry.RegisterStoredEventPair<KnownTrackDiscoveryStarted, KnownTrackDiscoveryStartedEventDataRecordDto>(
            nameof(KnownTrackDiscoveryStarted),
            domainEvent => new KnownTrackDiscoveryStartedEventDataRecordDto(
                domainEvent.TrackId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.Reason,
                domainEvent.StartedAt),
            dto => new KnownTrackDiscoveryStarted(
                TrackId.From(dto.TrackId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.Reason,
                dto.StartedAtUtc),
            domainEvent => domainEvent.StartedAt);

        registry.RegisterStoredEventPair<KnownTrackDiscoveryCompleted, KnownTrackDiscoveryCompletedEventDataRecordDto>(
            nameof(KnownTrackDiscoveryCompleted),
            domainEvent => new KnownTrackDiscoveryCompletedEventDataRecordDto(
                domainEvent.TrackId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.Reason,
                domainEvent.CompletedAt),
            dto => new KnownTrackDiscoveryCompleted(
                TrackId.From(dto.TrackId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.Reason,
                dto.CompletedAtUtc),
            domainEvent => domainEvent.CompletedAt);

        registry.RegisterStoredEventPair<KnownTrackDiscoveryDeferred, KnownTrackDiscoveryDeferredEventDataRecordDto>(
            nameof(KnownTrackDiscoveryDeferred),
            domainEvent => new KnownTrackDiscoveryDeferredEventDataRecordDto(
                domainEvent.TrackId.Value,
                domainEvent.EstimatedRetryAfterSeconds,
                domainEvent.EarliestExpectedCompletionAt,
                domainEvent.Reason,
                domainEvent.DeferredAt),
            dto => new KnownTrackDiscoveryDeferred(
                TrackId.From(dto.TrackId),
                dto.EstimatedRetryAfterSeconds,
                dto.EarliestExpectedCompletionAt,
                dto.Reason,
                dto.DeferredAtUtc),
            domainEvent => domainEvent.DeferredAt);

        registry.RegisterStoredEventPair<KnownTrackDiscoveryFailed, KnownTrackDiscoveryFailedEventDataRecordDto>(
            nameof(KnownTrackDiscoveryFailed),
            domainEvent => new KnownTrackDiscoveryFailedEventDataRecordDto(
                domainEvent.TrackId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.Reason,
                domainEvent.FailedAt),
            dto => new KnownTrackDiscoveryFailed(
                TrackId.From(dto.TrackId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.Reason,
                dto.FailedAtUtc),
            domainEvent => domainEvent.FailedAt);

        registry.RegisterStoredEventPair<KnownArtistDiscoveryStarted, KnownArtistDiscoveryStartedEventDataRecordDto>(
            nameof(KnownArtistDiscoveryStarted),
            domainEvent => new KnownArtistDiscoveryStartedEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.Reason,
                domainEvent.StartedAt),
            dto => new KnownArtistDiscoveryStarted(
                ArtistId.From(dto.ArtistId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.Reason,
                dto.StartedAtUtc),
            domainEvent => domainEvent.StartedAt);

        registry.RegisterStoredEventPair<KnownArtistDiscoveryCompleted, KnownArtistDiscoveryCompletedEventDataRecordDto>(
            nameof(KnownArtistDiscoveryCompleted),
            domainEvent => new KnownArtistDiscoveryCompletedEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.SourceProvider.ToString(),
                domainEvent.Reason,
                domainEvent.CompletedAt,
                domainEvent.ArtistName,
                domainEvent.SourceArtistId),
            dto => new KnownArtistDiscoveryCompleted(
                ArtistId.From(dto.ArtistId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                Enum.Parse<LookupSource>(dto.SourceProvider, ignoreCase: true),
                dto.Reason,
                dto.CompletedAtUtc,
                dto.ArtistName,
                dto.SourceArtistId),
            domainEvent => domainEvent.CompletedAt);

        registry.RegisterStoredEventPair<KnownArtistDiscoveryDeferred, KnownArtistDiscoveryDeferredEventDataRecordDto>(
            nameof(KnownArtistDiscoveryDeferred),
            domainEvent => new KnownArtistDiscoveryDeferredEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.EstimatedRetryAfterSeconds,
                domainEvent.EarliestExpectedCompletionAt,
                domainEvent.Reason,
                domainEvent.DeferredAt),
            dto => new KnownArtistDiscoveryDeferred(
                ArtistId.From(dto.ArtistId),
                dto.EstimatedRetryAfterSeconds,
                dto.EarliestExpectedCompletionAt,
                dto.Reason,
                dto.DeferredAtUtc),
            domainEvent => domainEvent.DeferredAt);

        registry.RegisterStoredEventPair<KnownArtistDiscoveryFailed, KnownArtistDiscoveryFailedEventDataRecordDto>(
            nameof(KnownArtistDiscoveryFailed),
            domainEvent => new KnownArtistDiscoveryFailedEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.Reason,
                domainEvent.FailedAt),
            dto => new KnownArtistDiscoveryFailed(
                ArtistId.From(dto.ArtistId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.Reason,
                dto.FailedAtUtc),
            domainEvent => domainEvent.FailedAt);

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

        registry.RegisterStoredEventPair<KnownAlbumDiscoveryStarted, KnownAlbumDiscoveryStartedEventDataRecordDto>(
            nameof(KnownAlbumDiscoveryStarted),
            domainEvent => new KnownAlbumDiscoveryStartedEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.AlbumId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.Reason,
                domainEvent.StartedAt),
            dto => new KnownAlbumDiscoveryStarted(
                ArtistId.From(dto.ArtistId),
                AlbumId.From(dto.AlbumId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.Reason,
                dto.StartedAtUtc),
            domainEvent => domainEvent.StartedAt);

        registry.RegisterStoredEventPair<KnownAlbumDiscoveryCompleted, KnownAlbumDiscoveryCompletedEventDataRecordDto>(
            nameof(KnownAlbumDiscoveryCompleted),
            domainEvent => new KnownAlbumDiscoveryCompletedEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.AlbumId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.SourceProvider.ToString(),
                domainEvent.Reason,
                domainEvent.CompletedAt,
                domainEvent.AlbumTitle,
                domainEvent.ArtistName,
                domainEvent.SourceAlbumId,
                domainEvent.SourceArtistId,
                domainEvent.ReleaseDate),
            dto => new KnownAlbumDiscoveryCompleted(
                ArtistId.From(dto.ArtistId),
                AlbumId.From(dto.AlbumId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                Enum.Parse<LookupSource>(dto.SourceProvider, ignoreCase: true),
                dto.Reason,
                dto.CompletedAtUtc,
                dto.AlbumTitle,
                dto.ArtistName,
                dto.SourceAlbumId,
                dto.SourceArtistId,
                dto.ReleaseDate),
            domainEvent => domainEvent.CompletedAt);

        registry.RegisterStoredEventPair<KnownAlbumDiscoveryDeferred, KnownAlbumDiscoveryDeferredEventDataRecordDto>(
            nameof(KnownAlbumDiscoveryDeferred),
            domainEvent => new KnownAlbumDiscoveryDeferredEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.AlbumId.Value,
                domainEvent.EstimatedRetryAfterSeconds,
                domainEvent.EarliestExpectedCompletionAt,
                domainEvent.Reason,
                domainEvent.DeferredAt),
            dto => new KnownAlbumDiscoveryDeferred(
                ArtistId.From(dto.ArtistId),
                AlbumId.From(dto.AlbumId),
                dto.EstimatedRetryAfterSeconds,
                dto.EarliestExpectedCompletionAt,
                dto.Reason,
                dto.DeferredAtUtc),
            domainEvent => domainEvent.DeferredAt);

        registry.RegisterStoredEventPair<KnownAlbumDiscoveryFailed, KnownAlbumDiscoveryFailedEventDataRecordDto>(
            nameof(KnownAlbumDiscoveryFailed),
            domainEvent => new KnownAlbumDiscoveryFailedEventDataRecordDto(
                domainEvent.ArtistId.Value,
                domainEvent.AlbumId.Value,
                domainEvent.Priority.ToString(),
                domainEvent.Reason,
                domainEvent.FailedAt),
            dto => new KnownAlbumDiscoveryFailed(
                ArtistId.From(dto.ArtistId),
                AlbumId.From(dto.AlbumId),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.Reason,
                dto.FailedAtUtc),
            domainEvent => domainEvent.FailedAt);

        registry.RegisterStoredEventPair<CatalogLookupRequested, AlbumCatalogLookupRequestedEventDataRecordDto>(
            nameof(CatalogLookupRequested),
            domainEvent => new AlbumCatalogLookupRequestedEventDataRecordDto(
                domainEvent.ArtistId?.Value,
                domainEvent.AlbumId.Value,
                domainEvent.RequestedAt,
                domainEvent.CorrelationId.Value),
            dto => new CatalogLookupRequested(
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
                    MusicSearchKind.UnifiedSearch => LookupCriteria.Query(
                        dto.Query ?? throw new InvalidOperationException("Stored unified streaming locations event requires a query.")),
                    MusicSearchKind.Isrc => LookupCriteria.ExactIsrc(
                        dto.Isrc ?? throw new InvalidOperationException("Stored ISRC streaming locations event requires an ISRC.")),
                    MusicSearchKind.TrackArtistAlbum => LookupCriteria.ByTrackArtistAlbum(
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
                domainEvent.Playback?.ToString(),
                domainEvent.TrustLevel,
                domainEvent.RiskScore,
                domainEvent.RequestedAt,
                domainEvent.CorrelationId.Value),
            dto => new DiscoveryRequested(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                dto.Playback is null ? null : PlaybackProviderFilter.Parse(dto.Playback),
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

        registry.RegisterStoredEventPair<WorkRejected, DiscoveryRejectedEventDataRecordDto>(
            nameof(WorkRejected),
            domainEvent => new DiscoveryRejectedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.WillBeLookedUp,
                domainEvent.Reason,
                domainEvent.RejectedAt),
            dto => new WorkRejected(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                dto.WillBeLookedUp,
                dto.Reason,
                dto.RejectedAtUtc),
            domainEvent => domainEvent.RejectedAt);

        registry.RegisterStoredEventPair<WorkAttemptFailed, DiscoveryFailedEventDataRecordDto>(
            nameof(WorkAttemptFailed),
            domainEvent => new DiscoveryFailedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.WillBeLookedUp,
                domainEvent.Reason,
                domainEvent.FailedAt),
            dto => new WorkAttemptFailed(
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

        registry.RegisterStoredEventPair<WorkCompleted, DiscoveryCompletedEventDataRecordDto>(
            nameof(WorkCompleted),
            domainEvent => new DiscoveryCompletedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.Priority.ToString(),
                domainEvent.WillBeLookedUp,
                domainEvent.Reason,
                domainEvent.CompletedAt),
            dto => new WorkCompleted(
                DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria),
                Enum.Parse<LookupPriorityBand>(dto.Priority, ignoreCase: true),
                dto.WillBeLookedUp,
                dto.Reason,
                dto.CompletedAtUtc),
            domainEvent => domainEvent.CompletedAt);

        registry.RegisterStoredEventPair<CatalogCandidateIdentified, CatalogCandidateIdentifiedEventDataRecordDto>(
            nameof(CatalogCandidateIdentified),
            domainEvent => new CatalogCandidateIdentifiedEventDataRecordDto(
                DiscoveryQueryKey.StableValueFor(domainEvent.SearchCriteria),
                domainEvent.MusicCatalogId.Value,
                domainEvent.TrustLevel,
                domainEvent.RiskScore,
                domainEvent.StartedAt,
                domainEvent.CorrelationId.Value),
            dto => new CatalogCandidateIdentified(
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
