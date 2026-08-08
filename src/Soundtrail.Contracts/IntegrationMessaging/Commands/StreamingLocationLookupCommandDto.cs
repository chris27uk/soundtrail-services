using Soundtrail.Contracts.Common;

namespace Soundtrail.Contracts.IntegrationMessaging.Commands;

public sealed record StreamingLocationLookupCommandDto(
    string CommandId,
    string CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBandDto Priority,
    string LookupKind,
    string TrackId,
    string Provider);
