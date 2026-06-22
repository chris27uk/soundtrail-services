using Raven.Client.Documents.Session;
using Soundtrail.Contracts.Common;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Infrastructure.Messaging;
using Wolverine.Attributes;

namespace Soundtrail.Services.Enrichment.DiscoveryPlanner.Features.JustInTimeScheduling.Adapters;

public sealed class CatalogSearchAttemptListener(CatalogSearchAttemptHandler handler)
{
    [WolverineHandler]
    [Transactional]
    public async Task<object[]> Handle(
        CatalogSearchAttemptDto requestDto,
        IAsyncDocumentSession _,
        CancellationToken cancellationToken = default)
    {
        var request = new CatalogSearchAttempt(
            CatalogSearchCriteria.From(requestDto.Criteria),
            NormalizedSearchQuery.FromText(requestDto.Query),
            requestDto.TrustLevel,
            requestDto.RiskScore,
            requestDto.OccurredAt,
            CorrelationId.From(requestDto.CorrelationId));
        var result = await handler.Handle(request, cancellationToken);
        return result.Commands.Select(command => command.ToMessage()).ToArray();
    }
}
