namespace Soundtrail.Contracts.EventSourcing;

public sealed record ArtistCatalogLookupRequestedEventDataRecordDto(
    string ArtistId,
    DateTimeOffset RequestedAtUtc,
    string CorrelationId);
