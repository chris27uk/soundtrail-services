using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record DispatchLookupWorkCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBandDto Priority,
    string TargetKind,
    string TargetValue,
    string? TargetItemKind,
    int SearchTypes);
