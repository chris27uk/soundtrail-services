using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Services.Enrichment.Orchestrator.Shared
{
    public static class RequestExtensions
    {
        public static DiscoveryHistory.SearchRequestContext ToAggregateContext(this IPrioritisedMessage request)
        {
            return new DiscoveryHistory.SearchRequestContext(
                request.Id,
                request.TrustLevel ?? 0,
                request.RiskScore ?? 0,
                request.RequestedAt,
                request.CorrelationId);
        }
    }
}
