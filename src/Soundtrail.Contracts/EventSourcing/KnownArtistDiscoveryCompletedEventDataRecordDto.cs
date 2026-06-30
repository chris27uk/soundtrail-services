namespace Soundtrail.Contracts.EventSourcing;

public sealed record KnownArtistDiscoveryCompletedEventDataRecordDto(
    string ArtistId,
    string Priority,
    string SourceProvider,
    string Reason,
    DateTimeOffset CompletedAtUtc,
    string ArtistName,
    string? SourceArtistId) : RavenEventBodyDto;
