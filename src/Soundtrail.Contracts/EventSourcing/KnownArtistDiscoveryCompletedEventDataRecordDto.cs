namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownArtistDiscoveryCompletedEventDataRecordDto(
    string ArtistId,
    string Priority,
    string Reason,
    DateTimeOffset CompletedAtUtc) : RavenEventBodyDto;
