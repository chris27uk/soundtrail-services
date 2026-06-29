namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownArtistDiscoveryStartedEventDataRecordDto(
    string ArtistId,
    string Priority,
    string Reason,
    DateTimeOffset StartedAtUtc) : RavenEventBodyDto;
