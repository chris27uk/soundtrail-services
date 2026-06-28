using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested;

public sealed class RecordCatalogSearchCandidateHandler(
    IEventStreamRepository<DiscoveryQueryKey, IDomainEvent> discoveryRepository) : IHandler<RecordCatalogSearchCandidateCommand>
{
    public async Task Handle(
        RecordCatalogSearchCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        var loaded = await CatalogSearchStarted.LoadAsync(
            discoveryRepository,
            command.SearchCriteria,
            cancellationToken);

        loaded.Aggregate.Record(
            command.MusicCatalogId,
            command.TrustLevel,
            command.RiskScore,
            command.OccurredAt,
            command.CorrelationId);

        await loaded.Aggregate.SaveAsync(discoveryRepository, loaded.Stream, cancellationToken);
    }
}
