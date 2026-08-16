using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnArtistCatalogChanged.Adapters;

internal sealed class ArtistCatalogChangedCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/artist-catalog-changed";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "artist-catalog-stream"
            && x.ProjectionHint != "bulk-import";

    protected override bool IsSubscriptionDefinitionStale(Raven.Client.Documents.Subscriptions.SubscriptionState state) =>
        !state.Query.Contains("bulk-import", StringComparison.Ordinal);

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<ArtistCatalogChangedProjectorHandler>();
        await handler.Handle(ArtistId.From(storedEvent.StreamId), cancellationToken);
    }
}
