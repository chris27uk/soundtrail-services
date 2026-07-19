using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Assesment;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicDataRequested;

public sealed class WorkRequestedProjectorHandler(ICommandBus commandBus)
{
    public Task Handle(WorkRequested @event, CancellationToken cancellationToken = default)
    {
        var command = new AssessWorkCommand(
            CommandId: @event.SubsequentDeterministicId("AssessWork"),
            CorrelationId: @event.CorrelationId,
            CreatedAt: @event.RequestedAt,
            Target: @event.Target,
            Priority: @event.Priority,
            TrustLevel: @event.TrustLevel,
            RiskScore: @event.RiskScore);

        return commandBus.SendAsync(command, cancellationToken);
    }
}
