namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownArtistDiscoveryFailedEventDataRecordDto(
    string ArtistId,
    string Priority,
    string Reason,
    DateTimeOffset FailedAtUtc) : RavenEventBodyDto;
