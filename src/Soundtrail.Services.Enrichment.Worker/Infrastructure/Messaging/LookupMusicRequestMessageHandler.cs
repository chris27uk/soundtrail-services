using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Features.Search.Queueing;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class LookupMusicRequestMessageHandler(LookupSchedulerOrchestrator orchestrator)
{
    public Task Handle(
        LookupMusicRequest request,
        CancellationToken cancellationToken) =>
        orchestrator.HandleAsync(request, cancellationToken);
}
