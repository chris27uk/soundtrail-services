using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Adapters.Api.Registrations;

public sealed class SearchCatalogRequestedToCatalogSearchAttemptTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.RegisterPair<SearchCatalogRequested, CatalogSearchAttemptDto>(
            request =>
            {
                var query = request.SearchCriteria.UnifiedQuery ?? string.Empty;

                return new CatalogSearchAttemptDto(
                    DiscoveryQueryKey.StableValueFor(request.SearchCriteria),
                    query,
                    request.Playback.ToString(),
                    request.TrustLevel,
                    request.RiskScore,
                    request.OccurredAt,
                    request.CorrelationId.Value);
            },
            dto =>
                new SearchCatalogRequested(
                    !string.IsNullOrWhiteSpace(dto.Criteria)
                        ? DiscoveryQueryKey.ToMusicSearchCriteria(dto.Criteria)
                        : MusicSearchCriteria.ByQuery(dto.Query),
                    PlaybackProviderFilter.Parse(dto.Playback),
                    dto.TrustLevel,
                    dto.RiskScore,
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)));
    }
}
