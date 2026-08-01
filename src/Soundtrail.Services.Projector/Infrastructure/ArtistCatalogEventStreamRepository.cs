using Raven.Client.Documents;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;

namespace Soundtrail.Services.Internal.Projector.Infrastructure;

public sealed class ArtistCatalogEventStreamRepository(
    IDocumentStore documentStore,
    ITypeRegistry typeRegistry) : IEventStreamRepository<ArtistId>
{
    public async Task<LoadedEventStream<ArtistId>> LoadAsync(ArtistId streamId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var repository = new RavenEventStreamRepository<ArtistId>(session, typeRegistry, "artist-catalog-stream");
        return await repository.LoadAsync(streamId, cancellationToken);
    }

    public async Task<AppendResult> AppendAsync(
        LoadedEventStream<ArtistId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var repository = new RavenEventStreamRepository<ArtistId>(session, typeRegistry, "artist-catalog-stream");
        return await repository.AppendAsync(stream, events, operationId, cancellationToken);
    }
}
