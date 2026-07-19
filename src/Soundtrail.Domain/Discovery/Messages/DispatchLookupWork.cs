using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;

namespace Soundtrail.Domain.Discovery.Commands;

public record DispatchLookupWork(
    EnrichmentTarget Target,
    LookupPriorityBand Priority,
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt) : ICommand;
