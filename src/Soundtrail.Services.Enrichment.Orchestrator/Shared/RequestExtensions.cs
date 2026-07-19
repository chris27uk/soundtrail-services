using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared
{
    public static class RequestExtensions
    {
        public static DiscoveryHistory.SearchRequestContext ToAggregateContext(this IPrioritisedCommand request)
        {
            return new DiscoveryHistory.SearchRequestContext(
                request.CommandId,
                request.TrustLevel ?? 0,
                request.RiskScore ?? 0,
                request.RequestedAt,
                request.CorrelationId);
        }
    }
}
