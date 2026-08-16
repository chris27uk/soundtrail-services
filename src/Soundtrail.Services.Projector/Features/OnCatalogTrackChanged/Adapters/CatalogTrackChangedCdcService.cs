using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogTrackChanged.Adapters;

internal sealed class CatalogTrackChangedCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/catalog-track-changed";

    protected override System.Linq.Expressions.Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "artist-catalog-stream"
             && x.ProjectionHint != "bulk-import"
             && (x.EventType == "track-discovered" || x.EventType == "streaming-location-discovered");

    protected override bool IsSubscriptionDefinitionStale(Raven.Client.Documents.Subscriptions.SubscriptionState state) =>
        !state.Query.Contains("streaming-location-discovered", StringComparison.Ordinal)
        || !state.Query.Contains("bulk-import", StringComparison.Ordinal);

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<CatalogTrackChangedProjectorHandler>();
        var body = storedEvent.Body
            ?? throw new InvalidOperationException($"{storedEvent.EventType} events must include a body.");

        if (string.Equals(storedEvent.EventType, "track-discovered", StringComparison.Ordinal))
        {
            var trackDiscovered = TypeTranslationRegistry.Default.ToDomainObject<TrackDiscovered>(body);
            await handler.Handle(trackDiscovered.Track.TrackId, cancellationToken);
            return;
        }

        var streamingLocationDiscovered = TypeTranslationRegistry.Default.ToDomainObject<StreamingLocationDiscovered>(body);
        await handler.Handle(streamingLocationDiscovered.MusicCatalogId.AsTrack(), cancellationToken);
    }
}
