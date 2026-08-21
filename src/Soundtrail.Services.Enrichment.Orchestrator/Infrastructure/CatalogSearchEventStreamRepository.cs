using Raven.Client.Documents;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure;

public sealed class CatalogSearchEventStreamRepository(
    IDocumentStore documentStore,
    ITypeRegistry typeRegistry) : IEventStreamRepository<CatalogWorkId>
{
    public async Task<LoadedEventStream<CatalogWorkId>> LoadAsync(CatalogWorkId streamId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var repository = new RavenEventStreamRepository<CatalogWorkId>(session, typeRegistry, "catalog-stream");
        return await repository.LoadAsync(streamId, cancellationToken);
    }

    public async Task<AppendResult> AppendAsync(
        LoadedEventStream<CatalogWorkId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken,
        ProjectionHint? projectionHint = null,
        bool saveChanges = true)
    {
        using var session = documentStore.OpenAsyncSession();
        var repository = new RavenEventStreamRepository<CatalogWorkId>(session, typeRegistry, "catalog-stream");
        return await repository.AppendAsync(stream, events, operationId, cancellationToken, projectionHint, saveChanges);
    }
}
