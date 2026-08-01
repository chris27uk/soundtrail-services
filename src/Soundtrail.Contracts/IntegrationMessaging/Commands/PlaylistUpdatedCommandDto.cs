namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record PlaylistUpdatedCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    string Name,
    string[] TrackIds);
