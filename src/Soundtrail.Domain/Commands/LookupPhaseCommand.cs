using Soundtrail.Contracts.Common;
using Soundtrail.Domain;

namespace Soundtrail.Domain.Commands;

public abstract record LookupPhaseCommand(
    CommandId CommandId,
    MusicCatalogId MusicCatalogId,
    LookupPriorityBand Priority,
    DateTimeOffset CreatedAt,
    CorrelationId CorrelationId) : ICommand;
