using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Api.Infrastructure.Messaging
{
    public static class CatalogSearchAttemptMapper
    {
        public static CatalogSearchAttemptDto ToDto(CatalogSearchAttempt request)
        {
            return new CatalogSearchAttemptDto(
                request.Criteria,
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId);
        }

        public static CatalogSearchAttempt? FromDto(CatalogSearchAttemptDto request)
        {
            return new CatalogSearchAttempt(
                CatalogSearchCriteria.From(request.Criteria),
                request.Query,
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                request.CorrelationId);
        }
    }
}
