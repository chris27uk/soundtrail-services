namespace Soundtrail.Contracts.EventSourcing;

public sealed record DiscoveryRejectedEventDataRecordDto(
    string Criteria,
    bool WillBeLookedUp,
    string Reason,
    DateTimeOffset RejectedAtUtc);
