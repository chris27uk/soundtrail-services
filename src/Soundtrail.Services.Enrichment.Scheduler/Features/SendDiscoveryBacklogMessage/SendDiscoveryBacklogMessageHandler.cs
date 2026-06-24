using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Services.Enrichment.Scheduler.Features.SendDiscoveryBacklogMessage.Support;

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
        return new RunDiscoveryBacklogSchedulingCommand(RunDiscoveryBacklogSchedulingCommand.Id(now), CorrelationId.New(), now, LookupPriorityBand.Low, now, BatchSize);
    }
}
