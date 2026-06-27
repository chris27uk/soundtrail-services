using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Translators.Discovery;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;

public sealed class CatalogSearchRequestedListener(CatalogSearchRequestedHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task Handle(
        CatalogSearchAttemptDto requestDto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var request = new CatalogSearchRequested(
            MusicSearchTermPersistentIdTranslator.ToSearchOrSeekDomainObject(requestDto.Criteria),
            PlaybackProviderFilter.Parse(requestDto.Playback),
            requestDto.TrustLevel,
            requestDto.RiskScore,
            requestDto.OccurredAt,
            CorrelationId.From(requestDto.CorrelationId));
        await handler.Handle(request, cancellationToken);
    }
}
