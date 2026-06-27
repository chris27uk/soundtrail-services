using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Discovery;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class SearchCatalogRequestedListener(SearchCatalogRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        CatalogSearchAttemptDto requestDto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        await handler.Handle(
            new SearchCatalogRequested(
                !string.IsNullOrWhiteSpace(requestDto.Criteria)
                    ? MusicSearchTermPersistentIdTranslator.ToDomainObject(requestDto.Criteria)
                    : MusicSearchCriteria.ByQuery(requestDto.Query),
                PlaybackProviderFilter.Parse(requestDto.Playback),
                requestDto.TrustLevel,
                requestDto.RiskScore,
                requestDto.OccurredAt,
                CorrelationId.From(requestDto.CorrelationId)),
            cancellationToken);
    }
}
