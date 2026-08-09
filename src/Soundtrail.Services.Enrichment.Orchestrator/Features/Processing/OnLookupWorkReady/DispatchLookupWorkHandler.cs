using System.Diagnostics;
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

        // Dispatch only the first attempt. LookupCompletedHandler advances the plan so
        // Completions never race on the same discovery stream.
        if (plan.Attempts.Count == 0)
        {
            return;
        }

        await commandBus.SendAsync(
            WorkerCommandFactory.Create(request, plan.Attempts[0]),
            cancellationToken);
    }
}
