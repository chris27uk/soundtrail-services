using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record KnownMusicDataRequestedCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBandDto Priority,
    string OperationKind,
    string OperationValue,
    string OperationItemKind,
    int? TrustLevel,
    int? RiskScore,
    DateTimeOffset RequestedAt);
