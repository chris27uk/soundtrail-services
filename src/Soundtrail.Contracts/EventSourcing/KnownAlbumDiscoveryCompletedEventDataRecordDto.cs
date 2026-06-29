namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownAlbumDiscoveryCompletedEventDataRecordDto(
    string ArtistId,
    string AlbumId,
    string Priority,
    string Reason,
    DateTimeOffset CompletedAtUtc) : RavenEventBodyDto;
