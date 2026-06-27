using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Api.Features.SearchCatalog.Adapters.Mappers;

internal static class CatalogSearchDiscoveryEventRecordMapper
{
    public static IReadOnlyList<DiscoveryQueryStoredEventRecordDto> ToStoredEvents(
        MusicSearchCriteria searchCriteria,
        IReadOnlyCollection<IDomainEvent> events,
        int startingVersion) =>
        events.Select((@event, index) => ToStoredEvent(searchCriteria, @event, startingVersion + index + 1))
            .ToArray();

    public static IReadOnlyList<DiscoveryQueryStoredEventRecordDto> ToStoredEvents(
        KnownCatalogItem knownItem,
        IReadOnlyCollection<IDomainEvent> events,
        int startingVersion) =>
        events.Select((@event, index) => ToStoredEvent(knownItem, @event, startingVersion + index + 1))
            .ToArray();

    public static IDomainEvent ToDomainEvent(DiscoveryQueryStoredEventRecordDto dto) =>
        dto.EventType switch
        {
            nameof(TrackMetadataLookupRequested) => ToTrackMetadataLookupRequested(dto),
            nameof(ArtistCatalogLookupRequested) => ToArtistCatalogLookupRequested(dto),
            nameof(AlbumCatalogLookupRequested) => ToAlbumCatalogLookupRequested(dto),
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

    public static DateTimeOffset GetOccurredAtUtc(IDomainEvent @event) =>
        @event switch
        {
            TrackMetadataLookupRequested required => required.RequiredAt,
            ArtistCatalogLookupRequested requested => requested.RequestedAt,
            AlbumCatalogLookupRequested requested => requested.RequestedAt,
            StreamingLocationsRequired required => required.ObservedAt,
            DiscoveryRequested requested => requested.RequestedAt,
            DiscoveryPlanned planned => planned.PlannedAt,
            DiscoveryDeferred deferred => deferred.DeferredAt,
            DiscoveryRejected rejected => rejected.RejectedAt,
            DiscoveryFailed failed => failed.FailedAt,
            DiscoveryStarted started => started.StartedAt,
            DiscoveryCompleted completed => completed.CompletedAt,
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown discovery event.")
        };

    private static DiscoveryQueryStoredEventRecordDto ToStoredEvent(
        MusicSearchCriteria searchCriteria,
        IDomainEvent @event,
        int version) =>
        @event switch
        {
            TrackMetadataLookupRequested required => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(TrackMetadataLookupRequested),
                TrackMetadataLookupRequested = new TrackMetadataLookupRequestedEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(required.SearchCriteria),
                    required.TrustLevel,
                    required.RiskScore,
                    required.RequiredAt,
                    required.CorrelationId.Value),
                OccurredAtUtc = required.RequiredAt,
                CorrelationId = required.CorrelationId.Value
            },
            StreamingLocationsRequired required => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(StreamingLocationsRequired),
                StreamingLocationsRequired = new StreamingLocationsRequiredEventDataRecordDto(
                    required.MusicCatalogId.Value,
                    required.Priority.ToString(),
                    required.CorrelationId.Value,
                    required.SourceProvider.Value,
                    required.ObservedAt,
                    required.SearchCriteria.Kind,
                    required.SearchCriteria.Query,
                    required.SearchCriteria.Isrc,
                    required.SearchCriteria.Title,
                    required.SearchCriteria.Artist,
                    required.SearchCriteria.Album,
                    required.Hierarchy?.ArtistId?.Value,
                    required.Hierarchy?.AlbumId?.Value),
                OccurredAtUtc = required.ObservedAt,
                CorrelationId = required.CorrelationId.Value
            },
            DiscoveryRequested requested => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(DiscoveryRequested),
                DiscoveryRequested = new DiscoveryRequestedEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(requested.SearchCriteria),
                    requested.SearchCriteria.Query ?? string.Empty,
                    requested.TrustLevel,
                    requested.RiskScore,
                    requested.RequestedAt,
                    requested.CorrelationId.Value),
                OccurredAtUtc = requested.RequestedAt,
                CorrelationId = requested.CorrelationId.Value
            },
            DiscoveryPlanned planned => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(DiscoveryPlanned),
                DiscoveryPlanned = new DiscoveryPlannedEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(planned.SearchCriteria),
                    planned.Priority.ToString(),
                    planned.WillBeLookedUp,
                    planned.EstimatedRetryAfterSeconds,
                    planned.EarliestExpectedCompletionAt,
                    planned.Reason,
                    planned.PlannedAt),
                OccurredAtUtc = planned.PlannedAt
            },
            DiscoveryDeferred deferred => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(DiscoveryDeferred),
                DiscoveryDeferred = new DiscoveryDeferredEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(deferred.SearchCriteria),
                    deferred.WillBeLookedUp,
                    deferred.EstimatedRetryAfterSeconds,
                    deferred.EarliestExpectedCompletionAt,
                    deferred.Reason,
                    deferred.DeferredAt),
                OccurredAtUtc = deferred.DeferredAt
            },
            DiscoveryRejected rejected => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(DiscoveryRejected),
                DiscoveryRejected = new DiscoveryRejectedEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(rejected.SearchCriteria),
                    rejected.WillBeLookedUp,
                    rejected.Reason,
                    rejected.RejectedAt),
                OccurredAtUtc = rejected.RejectedAt
            },
            DiscoveryFailed failed => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(DiscoveryFailed),
                DiscoveryFailed = new DiscoveryFailedEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(failed.SearchCriteria),
                    failed.WillBeLookedUp,
                    failed.Reason,
                    failed.FailedAt),
                OccurredAtUtc = failed.FailedAt
            },
            DiscoveryStarted started => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(DiscoveryStarted),
                DiscoveryStarted = new DiscoveryStartedEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(started.SearchCriteria),
                    started.Priority.ToString(),
                    started.WillBeLookedUp,
                    started.Reason,
                    started.StartedAt),
                OccurredAtUtc = started.StartedAt
            },
            DiscoveryCompleted completed => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria), version),
                Criteria = MusicSearchTermPersistentIdTranslator.ToPersistentId(searchCriteria),
                Version = version,
                EventType = nameof(DiscoveryCompleted),
                DiscoveryCompleted = new DiscoveryCompletedEventDataRecordDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(completed.SearchCriteria),
                    completed.Priority.ToString(),
                    completed.WillBeLookedUp,
                    completed.Reason,
                    completed.CompletedAt),
                OccurredAtUtc = completed.CompletedAt
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown discovery event.")
        };

    private static DiscoveryQueryStoredEventRecordDto ToStoredEvent(
        KnownCatalogItem knownItem,
        IDomainEvent @event,
        int version)
    {
        var persistentId = MusicSearchTermPersistentIdTranslator.ToPersistentId(knownItem);

        return @event switch
        {
            ArtistCatalogLookupRequested requested => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(persistentId, version),
                Criteria = persistentId,
                Version = version,
                EventType = nameof(ArtistCatalogLookupRequested),
                ArtistCatalogLookupRequested = new ArtistCatalogLookupRequestedEventDataRecordDto(
                    requested.ArtistId.Value,
                    requested.RequestedAt,
                    requested.CorrelationId.Value),
                OccurredAtUtc = requested.RequestedAt,
                CorrelationId = requested.CorrelationId.Value
            },
            AlbumCatalogLookupRequested requested => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(persistentId, version),
                Criteria = persistentId,
                Version = version,
                EventType = nameof(AlbumCatalogLookupRequested),
                AlbumCatalogLookupRequested = new AlbumCatalogLookupRequestedEventDataRecordDto(
                    requested.ArtistId?.Value,
                    requested.AlbumId.Value,
                    requested.RequestedAt,
                    requested.CorrelationId.Value),
                OccurredAtUtc = requested.RequestedAt,
                CorrelationId = requested.CorrelationId.Value
            },
            StreamingLocationsRequired required => new DiscoveryQueryStoredEventRecordDto
            {
                Id = DiscoveryQueryStoredEventRecordDto.GetDocumentId(persistentId, version),
                Criteria = persistentId,
                Version = version,
                EventType = nameof(StreamingLocationsRequired),
                StreamingLocationsRequired = new StreamingLocationsRequiredEventDataRecordDto(
                    required.MusicCatalogId.Value,
                    required.Priority.ToString(),
                    required.CorrelationId.Value,
                    required.SourceProvider.Value,
                    required.ObservedAt,
                    required.SearchCriteria.Kind,
                    required.SearchCriteria.Query,
                    required.SearchCriteria.Isrc,
                    required.SearchCriteria.Title,
                    required.SearchCriteria.Artist,
                    required.SearchCriteria.Album,
                    required.Hierarchy?.ArtistId?.Value,
                    required.Hierarchy?.AlbumId?.Value),
                OccurredAtUtc = required.ObservedAt,
                CorrelationId = required.CorrelationId.Value
            },
            _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, "Unknown known catalog discovery event.")
        };
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

    private static TrackMetadataLookupRequested ToTrackMetadataLookupRequested(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.TrackMetadataLookupRequested
            ?? throw new InvalidOperationException("Missing track metadata lookup requested event data.");
        return new TrackMetadataLookupRequested(
            MusicSearchTermPersistentIdTranslator.ToDomainObject(data.Criteria),
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

    private static ArtistCatalogLookupRequested ToArtistCatalogLookupRequested(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.ArtistCatalogLookupRequested
            ?? throw new InvalidOperationException("Missing artist catalog lookup requested event data.");
        return new ArtistCatalogLookupRequested(
            ArtistId.From(data.ArtistId),
            data.RequestedAtUtc,
            CorrelationId.From(data.CorrelationId));
    }

    private static AlbumCatalogLookupRequested ToAlbumCatalogLookupRequested(DiscoveryQueryStoredEventRecordDto dto)
    {
        var data = dto.AlbumCatalogLookupRequested
            ?? throw new InvalidOperationException("Missing album catalog lookup requested event data.");
        return new AlbumCatalogLookupRequested(
            data.ArtistId is null ? null : ArtistId.From(data.ArtistId),
            AlbumId.From(data.AlbumId),
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
