using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record UnknownMusicDataRequestedCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBandDto Priority,
    string Query,
    int SearchTypes,
    int TrustLevel,
    int RiskScore,
    DateTimeOffset RequestedAt);
