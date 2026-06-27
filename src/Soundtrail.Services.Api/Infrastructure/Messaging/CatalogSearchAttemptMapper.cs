using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Discovery;

namespace Soundtrail.Services.Api.Infrastructure.Messaging
{
    public static class CatalogSearchAttemptMapper
    {
        public static CatalogSearchAttemptDto ToDto(SearchCatalogRequested requested)
        {
            var query = requested.SearchCriteria.Query ?? string.Empty;
            return new CatalogSearchAttemptDto(
                MusicSearchTermPersistentIdTranslator.ToPersistentId(requested.SearchCriteria),
                query,
                requested.Playback.ToString(),
                requested.TrustLevel,
                requested.RiskScore,
                requested.OccurredAt,
                requested.CorrelationId.Value);
        }

        public static SearchCatalogRequested FromDto(CatalogSearchAttemptDto request)
        {
            return new SearchCatalogRequested(
                !string.IsNullOrWhiteSpace(request.Criteria)
                    ? MusicSearchTermPersistentIdTranslator.ToDomainObject(request.Criteria)
                    : MusicSearchCriteria.ByQuery(request.Query),
                PlaybackProviderFilter.Parse(request.Playback),
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                CorrelationId.From(request.CorrelationId));
        }
    }
}
