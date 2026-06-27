namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record KnownArtistRequestedDto(
    string ArtistId,
    DateTimeOffset OccurredAt,
    string CorrelationId);
