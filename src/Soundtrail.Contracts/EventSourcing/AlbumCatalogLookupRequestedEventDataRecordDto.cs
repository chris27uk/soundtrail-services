namespace Soundtrail.Contracts.EventSourcing;

public sealed record AlbumCatalogLookupRequestedEventDataRecordDto(
    string? ArtistId,
    string AlbumId,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId);
