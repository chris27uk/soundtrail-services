using System.Diagnostics;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;

public sealed class LookupWorkReadyHandler(ICommandBus commandBus) : IHandler<DispatchLookupWork>
{
    public async Task Handle(IncomingMessage<DispatchLookupWork> context, CancellationToken cancellationToken = default)
    {
        var request = context.Message;
        var plan = LookupPlanningPolicy.Build(request);
        Activity.Current?.SetTag("soundtrail.lookup_attempt_count", plan.Attempts.Count);

        foreach (var command in plan.Attempts.Select(attempt => WorkerCommandFactory.Create(request, attempt)))
        {
            await commandBus.SendAsync(command, cancellationToken);
        }

        MessageTelemetry.AddCurrentEvent("dispatch-lookup-work.commands-published");
    }
}
