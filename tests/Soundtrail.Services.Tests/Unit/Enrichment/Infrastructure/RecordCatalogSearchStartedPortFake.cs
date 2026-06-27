using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Persistence;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

internal sealed class RecordCatalogSearchStartedPortFake(
    CatalogSearchDiscoveryRepositoryFake repository) : IRecordCatalogSearchStartedPort
{
    public async Task RecordAsync(
        CatalogSearchCriteria criteria,
        IReadOnlyList<MusicCatalogId> musicCatalogIds,
        int trustLevel,
        int riskScore,
        DateTimeOffset occurredAt,
        CorrelationId correlationId,
        CancellationToken cancellationToken)
    {
        var aggregate = await CatalogSearchStarted.LoadAsync(repository, criteria, cancellationToken);
        foreach (var musicCatalogId in musicCatalogIds)
        {
            aggregate.Record(musicCatalogId, trustLevel, riskScore, occurredAt, correlationId);
        }

        await aggregate.SaveAsync(repository, cancellationToken);
    }
}
