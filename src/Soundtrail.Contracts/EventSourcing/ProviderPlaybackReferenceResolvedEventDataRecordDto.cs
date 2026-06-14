namespace Soundtrail.Contracts.EventSourcing;

public sealed record ProviderPlaybackReferenceResolvedEventDataRecordDto(
    string Provider,
    string? ExternalId,
    string Url,
    string SourceProvider,
    DateTimeOffset ObservedAt);
