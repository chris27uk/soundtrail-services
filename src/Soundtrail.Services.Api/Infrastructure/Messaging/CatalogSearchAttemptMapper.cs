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
        public static CatalogSearchAttemptDto ToDto(CatalogSearchRequested requested)
        {
            var query = requested.Criteria.Match(
                onSearch: search => search.Query ?? string.Empty,
                onSeek: _ => string.Empty);

            return new CatalogSearchAttemptDto(
                MusicSearchTermPersistentIdTranslator.ToPersistentId(requested.Criteria),
                query,
                requested.Playback.ToString(),
                requested.TrustLevel,
                requested.RiskScore,
                requested.OccurredAt,
                requested.CorrelationId.Value);
        }

        public static CatalogSearchRequested? FromDto(CatalogSearchAttemptDto request)
        {
            return new CatalogSearchRequested(
                !string.IsNullOrWhiteSpace(request.Criteria)
                    ? MusicSearchTermPersistentIdTranslator.ToSearchOrSeekDomainObject(request.Criteria)
                    : MusicSeekOrSearchCriteria.FromSearch(MusicSearchCriteria.ByQuery(request.Query)),
                PlaybackProviderFilter.Parse(request.Playback),
                request.TrustLevel,
                request.RiskScore,
                request.OccurredAt,
                CorrelationId.From(request.CorrelationId));
        }
    }
}
