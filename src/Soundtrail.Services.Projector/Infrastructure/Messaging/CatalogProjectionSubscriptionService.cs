using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Contracts.EventSourcing;

namespace Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

internal sealed class CatalogProjectionSubscriptionService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/catalog";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "catalog-stream"
             && (x.EventType == "artist-discovered"
                 || x.EventType == "album-discovered"
                 || x.EventType == "track-discovered"
                 || x.EventType == "streaming-location-discovered"
                 || x.EventType == "playlist-tracks-discovered");

    protected override bool IsSubscriptionDefinitionStale(Raven.Client.Documents.Subscriptions.SubscriptionState state) =>
        state.Query.Contains("artist-catalog-stream", StringComparison.Ordinal);

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var dispatcher = serviceProvider.GetRequiredService<CatalogProjectionDispatcher>();
        await dispatcher.DispatchAsync(storedEvent, cancellationToken);
    }
}
