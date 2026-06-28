namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryFailedEventDataRecordDto(
    string Criteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset FailedAtUtc) : RavenEventBodyDto;
