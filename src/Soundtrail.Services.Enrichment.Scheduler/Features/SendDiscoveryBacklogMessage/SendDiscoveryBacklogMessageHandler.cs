using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Enrichment.Commands;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage;

public sealed class SendDiscoveryBacklogMessageHandler(
    ICommandBus commandBus,
    ITimeProvider timeProvider) : ISchedulerHandler
{
    private const int BatchSize = 25;
    
    public Task Handle(CancellationToken cancellationToken = default) => commandBus.SendAsync(this.NewScheduledMessage(), cancellationToken);

    private RunDiscoveryBacklogSchedulingCommand NewScheduledMessage()
    {
        var now = timeProvider.UtcNow;
        return new RunDiscoveryBacklogSchedulingCommand(
            CommandId.For($"RunDiscoveryBacklogScheduling:{now.ToUnixTimeMilliseconds()}"),
            now,
            CorrelationId.New(),
            BatchSize,
            LookupPriorityBand.Low);
    }
}
