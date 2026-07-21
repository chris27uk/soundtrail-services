using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkRequested;

public sealed class WorkRequestedProjectorHandler(ICommandBus commandBus)
{
    public Task Handle(WorkRequested @event, CancellationToken cancellationToken = default)
    {
        return SendAssessCommand(
            @event.SubsequentDeterministicId("AssessWork"),
            @event.CorrelationId,
            @event.RequestedAt,
            @event.Target,
            @event.Priority,
            @event.TrustLevel,
            @event.RiskScore,
            cancellationToken);
    }

    public Task Handle(WorkPriorityRaised @event, CancellationToken cancellationToken = default)
    {
        return SendAssessCommand(
            @event.SubsequentDeterministicId("AssessWork"),
            @event.CorrelationId,
            @event.RequestedAt,
            @event.Target,
            @event.Priority,
            @event.TrustLevel,
            @event.RiskScore,
            cancellationToken);
    }

    private Task SendAssessCommand(
        MessageId id,
        CorrelationId correlationId,
        DateTimeOffset createdAt,
        EnrichmentTarget target,
        LookupPriorityBand priority,
        int? trustLevel,
        int? riskScore,
        CancellationToken cancellationToken)
    {
        var command = new AssessWorkMessage(
            Id: id,
            CorrelationId: correlationId,
            CreatedAt: createdAt,
            Target: target,
            Priority: priority,
            TrustLevel: trustLevel,
            RiskScore: riskScore);

        return commandBus.SendAsync(command, cancellationToken);
    }
}
