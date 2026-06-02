using Soundtrail.Services.Enrichment.Features.Scheduling;
using Soundtrail.Services.Enrichment.Features.Scheduling.Models;
using Soundtrail.Services.Features.Search.Contracts;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;

public sealed class LookupMusicRequestMessageHandler(LookupSchedulerOrchestrator orchestrator)
{
    public Task Handle(
        ResolutionDemandSignal signal,
        CancellationToken cancellationToken)
    {
        var request = new LookupMusicRequest(
            signal.Query,
            TrustLevel: 0,
            RiskScore: 0,
            OccurredAt: DateTimeOffset.UtcNow,
            CorrelationId: signal.QueryId.Value);

        return orchestrator.HandleAsync(request, cancellationToken);
    }
}
