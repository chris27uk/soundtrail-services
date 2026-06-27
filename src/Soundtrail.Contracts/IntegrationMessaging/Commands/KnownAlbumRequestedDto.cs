namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record KnownAlbumRequestedDto(
    string AlbumId,
    DateTimeOffset OccurredAt,
    string CorrelationId);
