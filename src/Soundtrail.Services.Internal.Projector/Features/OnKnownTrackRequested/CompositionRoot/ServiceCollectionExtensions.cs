using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.Registry;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.Ports;

namespace Soundtrail.Services.Internal.Projector.Features.OnKnownTrackRequested.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOnKnownTrackRequestedFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ILoadKnownTrackRequestedMusicTrackPort, RavenLoadKnownTrackRequestedMusicTrack>();
        services.TryAddScoped<IEventStreamRepository<DiscoveryQueryKey, IDomainEvent>>(sp =>
            new RavenEventStreamRepository<DiscoveryQueryKey, IDomainEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                DiscoveryQueryEventStreamDefinition.Create()));
        services.TryAddScoped<KnownTrackRequestedHandler>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, KnownTrackRequestedSubscriptionHostedService>());
        return services;
    }
}
