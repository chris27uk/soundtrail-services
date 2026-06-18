namespace Soundtrail.Contracts.EventSourcing;

public sealed record ProviderReferenceDiscoveredEventDataRecordDto(
    string Provider,
    string? ExternalId,
    string Url,
    string SourceProvider,
    DateTimeOffset ObservedAt);
