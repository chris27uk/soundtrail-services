using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Discovery;
using Soundtrail.Translators.Registry;

namespace Soundtrail.Translators.Api.Registrations;

public sealed class SearchCatalogRequestedToCatalogSearchAttemptTranslationRegistration : ITypeTranslationRegistration
{
    public void Register(TypeTranslationRegistry registry)
    {
        registry.Register<SearchCatalogRequested, CatalogSearchAttemptDto>(
            translate: request =>
            {
                var query = request.SearchCriteria.Query ?? string.Empty;

                return new CatalogSearchAttemptDto(
                    MusicSearchTermPersistentIdTranslator.ToPersistentId(request.SearchCriteria),
                    query,
                    request.Playback.ToString(),
                    request.TrustLevel,
                    request.RiskScore,
                    request.OccurredAt,
                    request.CorrelationId.Value);
            });

        registry.Register<CatalogSearchAttemptDto, SearchCatalogRequested>(
            translate: dto =>
                new SearchCatalogRequested(
                    !string.IsNullOrWhiteSpace(dto.Criteria)
                        ? MusicSearchTermPersistentIdTranslator.ToDomainObject(dto.Criteria)
                        : MusicSearchCriteria.ByQuery(dto.Query),
                    PlaybackProviderFilter.Parse(dto.Playback),
                    dto.TrustLevel,
                    dto.RiskScore,
                    dto.OccurredAt,
                    CorrelationId.From(dto.CorrelationId)));
    }
}
