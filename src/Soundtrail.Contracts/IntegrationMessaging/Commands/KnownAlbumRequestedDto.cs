namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record KnownAlbumRequestedDto(
    string ArtistId,
    string AlbumId,
    DateTimeOffset OccurredAt,
    string CorrelationId);
