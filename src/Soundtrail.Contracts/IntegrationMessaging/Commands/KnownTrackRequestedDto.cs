namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record KnownTrackRequestedDto(
    string TrackId,
    string Playback,
    DateTimeOffset OccurredAt,
    string CorrelationId);
