namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownAlbumDiscoveryCompletedEventDataRecordDto(
    string ArtistId,
    string AlbumId,
    string Priority,
    string SourceProvider,
    string Reason,
    DateTimeOffset CompletedAtUtc,
    string AlbumTitle,
    string ArtistName,
    string? SourceAlbumId,
    string? SourceArtistId,
    DateOnly? ReleaseDate) : RavenEventBodyDto;
