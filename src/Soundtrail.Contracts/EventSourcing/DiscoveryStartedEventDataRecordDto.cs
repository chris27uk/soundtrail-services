namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryStartedEventDataRecordDto(
    string Criteria,
    string Priority,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset StartedAtUtc) : RavenEventBodyDto;
