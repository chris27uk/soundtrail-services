using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery.Aggregates;

namespace Soundtrail.Adapters.EventSourcing.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCatalogSearchEventStreamRepository(this IServiceCollection services)
    {
        services.TryAddScoped<IEventStreamRepository<CatalogWorkId>, CatalogSearchEventStreamRepository>();

        return services;
    }

    public static IServiceCollection AddArtistCatalogEventStreamRepository(this IServiceCollection services)
    {
        services.TryAddScoped<IEventStreamRepository<ArtistId>, ArtistCatalogEventStreamRepository>();

        return services;
    }
}

internal sealed class CatalogSearchEventStreamRepository(
    IAsyncDocumentSession session,
    ITypeRegistry typeRegistry) : IEventStreamRepository<CatalogWorkId>
{
    private readonly RavenEventStreamRepository<CatalogWorkId> inner = new(session, typeRegistry, "catalog-stream");

    public Task<LoadedEventStream<CatalogWorkId>> LoadAsync(CatalogWorkId streamId, CancellationToken cancellationToken) =>
        this.inner.LoadAsync(streamId, cancellationToken);

    public Task<AppendResult> AppendAsync(
        LoadedEventStream<CatalogWorkId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken,
        ProjectionHint? projectionHint = null) =>
        this.inner.AppendAsync(stream, events, operationId, cancellationToken, projectionHint);
}

internal sealed class ArtistCatalogEventStreamRepository(
    IAsyncDocumentSession session,
    ITypeRegistry typeRegistry) : IEventStreamRepository<ArtistId>
{
    private readonly RavenEventStreamRepository<ArtistId> inner = new(session, typeRegistry, "artist-catalog-stream");

    public Task<LoadedEventStream<ArtistId>> LoadAsync(ArtistId streamId, CancellationToken cancellationToken) =>
        this.inner.LoadAsync(streamId, cancellationToken);

    public Task<AppendResult> AppendAsync(
        LoadedEventStream<ArtistId> stream,
        IReadOnlyList<IDomainEvent> events,
        OperationId? operationId,
        CancellationToken cancellationToken,
        ProjectionHint? projectionHint = null) =>
        this.inner.AppendAsync(stream, events, operationId, cancellationToken, projectionHint);
}
