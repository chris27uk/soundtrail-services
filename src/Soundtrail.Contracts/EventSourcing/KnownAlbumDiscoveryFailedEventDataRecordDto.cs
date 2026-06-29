namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownAlbumDiscoveryFailedEventDataRecordDto(
    string ArtistId,
    string AlbumId,
    string Priority,
    string Reason,
    DateTimeOffset FailedAtUtc) : RavenEventBodyDto;
