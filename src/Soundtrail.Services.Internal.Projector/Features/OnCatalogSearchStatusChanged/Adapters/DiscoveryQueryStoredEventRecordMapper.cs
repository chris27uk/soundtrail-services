using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Events;
using Soundtrail.Domain.Model;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchStatusChanged.Adapters;

public static class DiscoveryQueryStoredEventRecordMapper
{
    public static VersionedCatalogSearchDiscoveryEvent ToDomainEvent(this DiscoveryQueryStoredEventRecordDto dto) =>
        new(dto.Version, ToDomainEventData(dto));

    private static IDomainEvent ToDomainEventData(DiscoveryQueryStoredEventRecordDto dto) =>
        dto.EventType switch
        {
            nameof(MusicMetadataRequired) => ToMusicMetadataRequired(dto),
            nameof(StreamingLocationsRequired) => ToStreamingLocationsRequired(dto),
            nameof(DiscoveryRequested) => ToDiscoveryRequested(dto),
            nameof(DiscoveryPlanned) => ToDiscoveryPlanned(dto),
            nameof(DiscoveryDeferred) => ToDiscoveryDeferred(dto),
            nameof(DiscoveryRejected) => ToDiscoveryRejected(dto),
            nameof(DiscoveryFailed) => ToDiscoveryFailed(dto),
            nameof(DiscoveryStarted) => ToDiscoveryStarted(dto),
            nameof(DiscoveryCompleted) => ToDiscoveryCompleted(dto),
            _ => throw new ArgumentOutOfRangeException(nameof(dto.EventType), dto.EventType, "Unknown discovery event type.")
        };

    private static MusicMetadataRequired ToMusicMetadataRequired(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.MusicMetadataRequired
            ?? throw new InvalidOperationException("Missing music metadata required event data.");
        return new MusicMetadataRequired(
            MusicSearchTermPersistentIdTranslator.ToSearchOrSeekDomainObject(data.Criteria),
            data.TrustLevel,
            data.RiskScore,
            data.RequiredAtUtc,
            CorrelationId.From(data.CorrelationId));
    }

    private static StreamingLocationsRequired ToStreamingLocationsRequired(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.StreamingLocationsRequired
            ?? throw new InvalidOperationException("Missing streaming locations required event data.");
        return new StreamingLocationsRequired(
            MusicCatalogId.From(data.MusicCatalogId),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            CorrelationId.From(data.CorrelationId),
            ProviderName.From(data.SourceProvider),
            data.ObservedAt,
            data.SearchKind switch
            {
                MusicSearchKind.UnifiedSearch => MusicSearchCriteria.ByQuery(data.Query ?? throw new InvalidOperationException("Query search term is required.")),
                MusicSearchKind.Isrc => MusicSearchCriteria.ByIsrc(data.Isrc ?? throw new InvalidOperationException("ISRC search term is required.")),
                MusicSearchKind.TrackArtistAlbum => MusicSearchCriteria.ByTrackArtistAlbum(
                    data.Title ?? throw new InvalidOperationException("Track title is required."),
                    data.Artist ?? throw new InvalidOperationException("Track artist is required."),
                    data.Album),
                _ => throw new InvalidOperationException($"Unsupported music search kind '{data.SearchKind}'.")
            },
            data.ArtistId is null && data.AlbumId is null
                ? null
                : new CatalogTrackHierarchy(
                    data.ArtistId is null ? null : ArtistId.From(data.ArtistId),
                    data.AlbumId is null ? null : AlbumId.From(data.AlbumId)));
    }

    private static DiscoveryRequested ToDiscoveryRequested(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryRequested
            ?? throw new InvalidOperationException("Missing discovery requested event data.");
        return new DiscoveryRequested(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
            data.TrustLevel,
            data.RiskScore,
            data.RequestedAtUtc,
            CorrelationId.From(data.CorrelationId));
    }

    private static DiscoveryPlanned ToDiscoveryPlanned(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryPlanned
            ?? throw new InvalidOperationException("Missing discovery planned event data.");
        return new DiscoveryPlanned(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.WillBeLookedUp,
            data.EstimatedRetryAfterSeconds,
            data.EarliestExpectedCompletionAt,
            data.Reason,
            data.PlannedAtUtc);
    }

    private static DiscoveryDeferred ToDiscoveryDeferred(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryDeferred
            ?? throw new InvalidOperationException("Missing discovery deferred event data.");
        return new DiscoveryDeferred(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
            data.WillBeLookedUp,
            data.EstimatedRetryAfterSeconds,
            data.EarliestExpectedCompletionAt,
            data.Reason,
            data.DeferredAtUtc);
    }

    private static DiscoveryRejected ToDiscoveryRejected(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryRejected
            ?? throw new InvalidOperationException("Missing discovery rejected event data.");
        return new DiscoveryRejected(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
            data.WillBeLookedUp,
            data.Reason,
            data.RejectedAtUtc);
    }

    private static DiscoveryFailed ToDiscoveryFailed(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryFailed
            ?? throw new InvalidOperationException("Missing discovery failed event data.");
        return new DiscoveryFailed(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
            data.WillBeLookedUp,
            data.Reason,
            data.FailedAtUtc);
    }

    private static DiscoveryStarted ToDiscoveryStarted(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryStarted
            ?? throw new InvalidOperationException("Missing discovery started event data.");
        return new DiscoveryStarted(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.WillBeLookedUp,
            data.Reason,
            data.StartedAtUtc);
    }

    private static DiscoveryCompleted ToDiscoveryCompleted(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.DiscoveryCompleted
            ?? throw new InvalidOperationException("Missing discovery completed event data.");
        return new DiscoveryCompleted(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
            Enum.Parse<LookupPriorityBand>(data.Priority, ignoreCase: true),
            data.WillBeLookedUp,
            data.Reason,
            data.CompletedAtUtc);
    }
}
