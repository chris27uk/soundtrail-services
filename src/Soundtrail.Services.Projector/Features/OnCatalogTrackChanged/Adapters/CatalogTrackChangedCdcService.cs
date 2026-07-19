using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged.Adapters;

internal sealed class CatalogTrackChangedCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/catalog-track-changed";

    protected override System.Linq.Expressions.Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "artist-catalog-stream" && x.EventType == "track-discovered";

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<CatalogTrackChangedProjectorHandler>();
        var trackDiscovered = TypeTranslationRegistry.Default.ToDomainObject<TrackDiscovered>(
            storedEvent.Body ?? throw new InvalidOperationException("TrackDiscovered events must include a body."));

        await handler.Handle(trackDiscovered.Track.TrackId, cancellationToken);
    }
}
