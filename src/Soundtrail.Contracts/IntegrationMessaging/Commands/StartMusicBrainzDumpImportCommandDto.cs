namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record StartMusicBrainzDumpImportCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset RequestedAt,
    string JobId,
    string DumpVersion);
