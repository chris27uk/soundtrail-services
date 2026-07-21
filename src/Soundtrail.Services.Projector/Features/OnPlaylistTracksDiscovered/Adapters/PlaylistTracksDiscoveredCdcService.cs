using System.Linq.Expressions;
using Microsoft.Extensions.DependencyInjection;
using Raven.Client.Documents;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Infrastructure.Messaging;

namespace Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;

internal sealed class PlaylistTracksDiscoveredCdcService(
    IServiceScopeFactory scopeFactory,
    IDocumentStore documentStore) : RavenEventSubscriptionBackgroundService(scopeFactory, documentStore)
{
    protected override string SubscriptionName => "projector/playlist-tracks-discovered";

    protected override Expression<Func<RavenStoredEventRecord, bool>> Filter =>
        x => x.AggregateType == "catalog-stream" && x.EventType == "playlist-tracks-discovered";

    protected override async Task HandleAsync(
        IServiceProvider serviceProvider,
        RavenStoredEventRecord storedEvent,
        CancellationToken cancellationToken)
    {
        var handler = serviceProvider.GetRequiredService<PlaylistTracksDiscoveredProjectorHandler>();
        var playlistTracksDiscovered = TypeTranslationRegistry.Default.ToDomainObject<PlaylistTracksDiscovered>(
            storedEvent.Body ?? throw new InvalidOperationException("PlaylistTracksDiscovered events must include a body."));

        await handler.Handle(playlistTracksDiscovered, cancellationToken);
    }
}
