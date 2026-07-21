using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchCandidateChanged.Adapters;

internal sealed class CatalogSearchCandidateChangedCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/catalog-search-candidate-changed";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "catalog-stream"
            && (x.EventType == "artist-discovered"
                || x.EventType == "album-discovered"
                || x.EventType == "track-discovered");

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<CatalogSearchCandidateChangedProjectorHandler>();

        switch (storedEvent.EventType)
        {
            case "artist-discovered":
                var artistDiscovered = TypeTranslationRegistry.Default.ToDomainObject<ArtistDiscovered>(
                    storedEvent.Body ?? throw new InvalidOperationException("ArtistDiscovered events must include a body."));
                await handler.Handle(artistDiscovered, cancellationToken);
                break;
            case "album-discovered":
                var albumDiscovered = TypeTranslationRegistry.Default.ToDomainObject<AlbumDiscovered>(
                    storedEvent.Body ?? throw new InvalidOperationException("AlbumDiscovered events must include a body."));
                await handler.Handle(albumDiscovered, cancellationToken);
                break;
            case "track-discovered":
                var trackDiscovered = TypeTranslationRegistry.Default.ToDomainObject<TrackDiscovered>(
                    storedEvent.Body ?? throw new InvalidOperationException("TrackDiscovered events must include a body."));
                await handler.Handle(trackDiscovered, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported catalog search candidate changed event type '{storedEvent.EventType}'.");
        }
    }
}
