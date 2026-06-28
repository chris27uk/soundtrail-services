namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryCompletedEventDataRecordDto(
    string Criteria,
    string Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset CompletedAtUtc) : RavenEventBodyDto;
