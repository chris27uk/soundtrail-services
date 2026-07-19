using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady;

public sealed class LookupWorkReadyHandler(ICommandBus commandBus) : IHandler<DispatchLookupWork>
{
    public async Task Handle(DispatchLookupWork request, CancellationToken cancellationToken = default)
    {
        var plan = LookupPlanningPolicy.Build(request);
        foreach (var command in plan.Lookups.Select(lookup => WorkerCommandFactory.Create(request, lookup)))
        {
            await commandBus.SendAsync(command, cancellationToken);
        }
    }
}
