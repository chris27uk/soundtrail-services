namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record CatalogLookupCompletedCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset RequestedAt,
    string ResultKind,
    string StreamId,
    string OriginalCommandId,
    DateTimeOffset CompletedAt,
    string? Reason,
    DateTimeOffset? DeferredUntil,
    CatalogLookupValueDto? Value,
    CatalogItemCommandDto? ExistingItem);

public sealed record CatalogLookupValueDto(
    string ValueKind,
    CatalogDiscoveryEntryCommandDto[]? CatalogEntries,
    TrackReferenceCommandDto[]? PlaylistTrackReferences,
    TrackStreamingLinkCommandDto? TrackStreamingLink);

public sealed record CatalogDiscoveryEntryCommandDto(
    string ArtistId,
    CatalogItemCommandDto Item);

public sealed record CatalogItemCommandDto(
    string Kind,
    string? ArtistId,
    string? ArtistName,
    string? ArtistImageUrl,
    string? AlbumId,
    string? AlbumTitle,
    string? SourceAlbumId,
    DateOnly? ReleaseDate,
    string? ArtworkUrl,
    string? TrackId,
    string? TrackTitle,
    string? TrackArtistName,
    string? TrackAlbumId,
    string? TrackAlbumTitle,
    int? DurationMs,
    string? Isrc,
    string? Mbid,
    string? ReleaseType,
    DateTimeOffset? UpdatedAt,
    IReadOnlyList<string>? SourceSystemIds = null);

public sealed record TrackReferenceCommandDto(
    string ArtistName,
    string TrackTitle);

public sealed record TrackStreamingLinkCommandDto(
    string ArtistId,
    string TrackId,
    string Provider,
    string? ExternalId,
    string Url,
    string SourceProvider,
    DateTimeOffset ObservedAt);
