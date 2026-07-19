using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Playlists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;

namespace Soundtrail.Adapters.TypeRegistry.Registrations;

public sealed class DiscoveryEventTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterStoredEventPair<WorkRequested, CatalogDiscoveryWorkRequestedEventDataRecordDto>(
            eventType: "work-requested",
            toDto: @event => new CatalogDiscoveryWorkRequestedEventDataRecordDto(
                GetResourceKind(@event.Target),
                GetResourceValue(@event.Target),
                GetResourceItemKind(@event.Target),
                @event.Priority.ToString(),
                @event.TrustLevel,
                @event.RiskScore,
                @event.RequestedAt,
                @event.CorrelationId.Value),
            toDomainObject: dto => new WorkRequested(
                ParseFilter(dto.ResourceKind, dto.ResourceValue, dto.ResourceItemKind),
                ParsePriority(dto.Priority),
                dto.TrustLevel,
                dto.RiskScore,
                dto.RequestedAtUtc,
                CorrelationId.From(dto.CorrelationId)),
            occurredAtUtc: @event => @event.RequestedAt,
            correlationId: @event => @event.CorrelationId.Value);

        registry.RegisterStoredEventPair<WorkScheduled, CatalogDiscoveryWorkScheduledEventDataRecordDto>(
            eventType: "work-scheduled",
            toDto: @event => new CatalogDiscoveryWorkScheduledEventDataRecordDto(
                @event.Target.NormalisedIdentifier,
                @event.Priority.ToString(),
                @event.NextEligibleAt,
                @event.EarliestExpectedCompletionAt,
                @event.Reason,
                @event.ScheduledAt),
            toDomainObject: dto => new WorkScheduled(
                ParseTargetByIdentifier(dto.MusicCatalogId),
                ParsePriority(dto.Priority),
                dto.NextEligibleAtUtc,
                dto.EarliestExpectedCompletionAt,
                dto.Reason,
                dto.ScheduledAtUtc),
            occurredAtUtc: @event => @event.ScheduledAt,
            correlationId: _ => null);

        registry.RegisterStoredEventPair<WorkDeferred, CatalogDiscoveryWorkDeferredEventDataRecordDto>(
            eventType: "work-deferred",
            toDto: @event => new CatalogDiscoveryWorkDeferredEventDataRecordDto(
                @event.Target.NormalisedIdentifier,
                @event.Priority.ToString(),
                @event.NextEligibleAt,
                @event.EstimatedRetryAfterSeconds,
                @event.Reason,
                @event.DeferredAt),
            toDomainObject: dto => new WorkDeferred(
                ParseTargetByIdentifier(dto.MusicCatalogId),
                ParsePriority(dto.Priority),
                dto.NextEligibleAtUtc,
                dto.EstimatedRetryAfterSeconds,
                dto.Reason,
                dto.DeferredAtUtc),
            occurredAtUtc: @event => @event.DeferredAt,
            correlationId: _ => null);

        registry.RegisterStoredEventPair<WorkIgnored, CatalogDiscoveryWorkIgnoredEventDataRecordDto>(
            eventType: "work-ignored",
            toDto: @event => new CatalogDiscoveryWorkIgnoredEventDataRecordDto(
                @event.Target.NormalisedIdentifier,
                @event.Priority.ToString(),
                @event.NextEligibleAt,
                @event.EstimatedRetryAfterSeconds,
                @event.EarliestExpectedCompletionAt,
                @event.Reason,
                @event.IgnoredAt),
            toDomainObject: dto => new WorkIgnored(
                ParseTargetByIdentifier(dto.MusicCatalogId),
                ParsePriority(dto.Priority),
                dto.NextEligibleAtUtc,
                dto.EstimatedRetryAfterSeconds,
                dto.EarliestExpectedCompletionAt,
                dto.Reason,
                dto.IgnoredAtUtc),
            occurredAtUtc: @event => @event.IgnoredAt,
            correlationId: _ => null);

        registry.RegisterStoredEventPair<WorkCompleted, CatalogDiscoveryWorkCompletedEventDataRecordDto>(
            eventType: "work-completed",
            toDto: @event => new CatalogDiscoveryWorkCompletedEventDataRecordDto(
                @event.Target.NormalisedIdentifier,
                @event.Priority.ToString(),
                @event.Reason,
                @event.CompletedAt),
            toDomainObject: dto => new WorkCompleted(
                ParseTargetByIdentifier(dto.MusicCatalogId),
                ParsePriority(dto.Priority),
                dto.Reason,
                dto.CompletedAtUtc),
            occurredAtUtc: @event => @event.CompletedAt,
            correlationId: _ => null);

        registry.RegisterStoredEventPair<WorkRejected, CatalogDiscoveryWorkRejectedEventDataRecordDto>(
            eventType: "work-rejected",
            toDto: @event => new CatalogDiscoveryWorkRejectedEventDataRecordDto(
                @event.Target.NormalisedIdentifier,
                @event.Priority.ToString(),
                @event.Reason,
                @event.RejectedAt),
            toDomainObject: dto => new WorkRejected(
                ParseTargetByIdentifier(dto.MusicCatalogId),
                ParsePriority(dto.Priority),
                dto.Reason,
                dto.RejectedAtUtc),
            occurredAtUtc: @event => @event.RejectedAt,
            correlationId: _ => null);

        registry.RegisterStoredEventPair<WorkAttemptFailed, CatalogDiscoveryWorkAttemptFailedEventDataRecordDto>(
            eventType: "work-attempt-failed",
            toDto: @event => new CatalogDiscoveryWorkAttemptFailedEventDataRecordDto(
                @event.Target.NormalisedIdentifier,
                @event.Reason,
                @event.FailedAt),
            toDomainObject: dto => new WorkAttemptFailed(
                ParseTargetByIdentifier(dto.MusicCatalogId),
                dto.Reason,
                dto.FailedAtUtc),
            occurredAtUtc: @event => @event.FailedAt,
            correlationId: _ => null);

    }

    private static string GetResourceKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem => "search-criteria",
            EnrichmentTarget.KnownCatalogItemOperation(var operation) => GetOperationResourceKind(operation),
            _ => throw new InvalidOperationException($"Unsupported enrichment filter '{target.GetType().Name}'.")
        };

    private static string GetResourceValue(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.SearchForUnknownCatalogItem(var searchCriteria) => searchCriteria.Query,
            EnrichmentTarget.KnownCatalogItemOperation(var operation) => GetOperationResourceValue(operation),
            _ => throw new InvalidOperationException($"Unsupported enrichment filter '{target.GetType().Name}'.")
        };

    private static string? GetResourceItemKind(EnrichmentTarget target) =>
        target switch
        {
            EnrichmentTarget.KnownCatalogItemOperation(var operation) => GetOperationResourceItemKind(operation),
            _ => null
        };

    private static EnrichmentTarget ParseFilter(string resourceKind, string resourceValue, string? resourceItemKind) =>
        resourceKind switch
        {
            "search-criteria" => new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria(resourceValue)),
            "streaming_location_for_track" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.StreamingLocationForTrack(ParseTrackId(resourceValue, resourceItemKind))),
            "child_albums_for_artist" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildAlbumsForArtist(ParseArtistId(resourceValue, resourceItemKind))),
            "child_tracks_for_artist" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(ParseArtistId(resourceValue, resourceItemKind))),
            "child_tracks_for_album" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForAlbum(ParseAlbumId(resourceValue, resourceItemKind))),
            "child_tracks_for_playlist" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForPlaylist(ParsePlaylistId(resourceValue, resourceItemKind))),
            _ => throw new InvalidOperationException($"Unsupported resource kind '{resourceKind}'.")
        };

    private static string GetOperationResourceKind(CatalogItemOperation operation) =>
        operation switch
        {
            CatalogItemOperation.ChildAlbumsForArtist => "child_albums_for_artist",
            CatalogItemOperation.ChildTracksForArtist => "child_tracks_for_artist",
            CatalogItemOperation.ChildTracksForAlbum => "child_tracks_for_album",
            CatalogItemOperation.ChildTracksForPlaylist => "child_tracks_for_playlist",
            CatalogItemOperation.StreamingLocationForTrack => "streaming_location_for_track",
            _ => throw new InvalidOperationException($"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };

    private static string GetOperationResourceValue(CatalogItemOperation operation) =>
        operation switch
        {
            CatalogItemOperation.ChildAlbumsForArtist(var artistId) => artistId.Value,
            CatalogItemOperation.ChildTracksForArtist(var artistId) => artistId.Value,
            CatalogItemOperation.ChildTracksForAlbum(var albumId) => albumId.StableValue,
            CatalogItemOperation.ChildTracksForPlaylist(var playlistId) => playlistId.Value,
            CatalogItemOperation.StreamingLocationForTrack(var trackId) => trackId.Value,
            _ => throw new InvalidOperationException($"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };

    private static string GetOperationResourceItemKind(CatalogItemOperation operation) =>
        operation switch
        {
            CatalogItemOperation.StreamingLocationForTrack => "track",
            CatalogItemOperation.ChildAlbumsForArtist => "artist",
            CatalogItemOperation.ChildTracksForArtist => "artist",
            CatalogItemOperation.ChildTracksForAlbum => "album",
            CatalogItemOperation.ChildTracksForPlaylist => "playlist",
            _ => throw new InvalidOperationException($"Unsupported catalog item operation '{operation.GetType().Name}'.")
        };

    private static TrackId ParseTrackId(string resourceValue, string? resourceItemKind)
    {
        if (resourceItemKind != "track")
        {
            throw new InvalidOperationException($"Unsupported resource item kind '{resourceItemKind}'.");
        }

        return TrackId.From(resourceValue);
    }

    private static ArtistId ParseArtistId(string resourceValue, string? resourceItemKind)
    {
        if (resourceItemKind != "artist")
        {
            throw new InvalidOperationException($"Unsupported resource item kind '{resourceItemKind}'.");
        }

        return ArtistId.From(resourceValue);
    }

    private static AlbumId ParseAlbumId(string resourceValue, string? resourceItemKind)
    {
        if (resourceItemKind != "album")
        {
            throw new InvalidOperationException($"Unsupported resource item kind '{resourceItemKind}'.");
        }

        return AlbumId.From(resourceValue);
    }

    private static PlaylistId ParsePlaylistId(string resourceValue, string? resourceItemKind)
    {
        if (resourceItemKind != "playlist")
        {
            throw new InvalidOperationException($"Unsupported resource item kind '{resourceItemKind}'.");
        }

        return PlaylistId.FromPlaylistName(resourceValue);
    }

    private static LookupPriorityBand ParsePriority(string priority) =>
        Enum.Parse<LookupPriorityBand>(priority, ignoreCase: true);

    private static EnrichmentTarget ParseTargetByIdentifier(string identifier)
    {
        if (identifier.StartsWith("search:", StringComparison.Ordinal))
        {
            return new EnrichmentTarget.SearchForUnknownCatalogItem(new SearchCriteria(identifier["search:".Length..]));
        }

        var separatorIndex = identifier.IndexOf(':');
        if (separatorIndex < 0)
        {
            throw new InvalidOperationException($"Unsupported target identifier '{identifier}'.");
        }

        var kind = identifier[..separatorIndex];
        var value = identifier[(separatorIndex + 1)..];

        return kind switch
        {
            "streaming_location_for_track" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.StreamingLocationForTrack(TrackId.From(value))),
            "child_albums_for_artist" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildAlbumsForArtist(ArtistId.From(value))),
            "child_tracks_for_artist" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForArtist(ArtistId.From(value))),
            "child_tracks_for_album" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForAlbum(AlbumId.From(value))),
            "child_tracks_for_playlist" => new EnrichmentTarget.KnownCatalogItemOperation(new CatalogItemOperation.ChildTracksForPlaylist(PlaylistId.FromPlaylistName(value))),
            _ => throw new InvalidOperationException($"Unsupported target identifier '{identifier}'.")
        };
    }
}
