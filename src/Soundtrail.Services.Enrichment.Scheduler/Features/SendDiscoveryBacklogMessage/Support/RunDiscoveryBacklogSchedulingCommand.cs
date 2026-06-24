using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage.Support;

public sealed record RunDiscoveryBacklogSchedulingCommand(
    CommandId CommandId,
    CorrelationId CorrelationId,
    DateTimeOffset CreatedAt,
    LookupPriorityBand Priority,
    DateTimeOffset Now,
    int Take) : ICommand
{
    public static CommandId Id(DateTimeOffset now) => CommandId.For($"RunDiscoveryBacklogScheduling:{now.ToUnixTimeMilliseconds()}");
}
