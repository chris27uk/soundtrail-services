namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record ImportMusicBrainzDumpShardCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset RequestedAt,
    string JobId,
    string Phase,
    int ShardId);
