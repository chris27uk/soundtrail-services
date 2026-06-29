namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownAlbumDiscoveryStartedEventDataRecordDto(
    string ArtistId,
    string AlbumId,
    string Priority,
    string Reason,
    DateTimeOffset StartedAtUtc) : RavenEventBodyDto;
