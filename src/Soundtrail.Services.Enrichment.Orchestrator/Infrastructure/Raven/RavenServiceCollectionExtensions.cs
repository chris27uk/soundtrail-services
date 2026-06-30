using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Session;
using Soundtrail.Adapters.Discovery;
using Soundtrail.Adapters.Enrichment;
using Soundtrail.Adapters.EventSourcing;
using Soundtrail.Adapters.MusicTrackEventStore;
using Soundtrail.Adapters.Registry;
using Soundtrail.Adapters.Registry.CompositionRoot;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Enrichment;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnMusicCatalogLookupAttempted.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnNextMusicTracksRequestedForLookup.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven.CatalogDiscoveryWork;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Idempotency;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Persistence;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;

namespace Soundtrail.Services.Enrichment.Orchestrator.Infrastructure.Raven;

public static class RavenServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerRavenDocumentStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddTypeTranslationsFromAssemblies(typeof(TypeTranslationRegistry).Assembly);
        services.Configure<RavenDbOptions>(configuration.GetSection(RavenDbOptions.SectionName));
        services.TryAddSingleton<IDocumentStore>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<RavenDbOptions>>().Value;
                var store = new DocumentStore
                {
                    Urls = options.Urls,
                    Database = options.Database,
                    Conventions = new DocumentConventions
                    {
                        FindCollectionName = type => type.Name
                    }
                };

                return store.Initialize();
            });
        services.TryAddScoped<IAsyncDocumentSession>(sp => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RavenDatabaseHostedService>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, RavenIndexesHostedService>());

        services.TryAddScoped<IEventStreamRepository<DiscoveryQueryKey, IDomainEvent>>(sp =>
            new RavenEventStreamRepository<DiscoveryQueryKey, IDomainEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                DiscoveryQueryEventStreamDefinition.Create()));
        services.TryAddScoped<IEventStreamRepository<MusicCatalogId, IDomainEvent>>(sp =>
            new RavenEventStreamRepository<MusicCatalogId, IDomainEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                CatalogDiscoveryWorkEventStreamDefinition.Create()));
        services.TryAddScoped<IEventStreamRepository<MusicCatalogLookupId, IDomainEvent>>(sp =>
            new RavenEventStreamRepository<MusicCatalogLookupId, IDomainEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                MusicCatalogLookupEventStreamDefinition.Create()));
        services.TryAddScoped<IEventStreamRepository<MusicCatalogId, IMusicTrackEvent>>(sp =>
            new RavenEventStreamRepository<MusicCatalogId, IMusicTrackEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                MusicTrackEventStreamDefinition.Create()));
        services.TryAddScoped<IEventStreamRepository<ArtistId, IDomainEvent>>(sp =>
            new RavenEventStreamRepository<ArtistId, IDomainEvent>(
                sp.GetRequiredService<IAsyncDocumentSession>(),
                sp.GetRequiredService<ITypeRegistry>(),
                ArtistCatalogEventStreamDefinition.Create()));
        services.TryAddScoped<ICatalogDiscoveryWorkSummaryStore, RavenCatalogDiscoveryWorkSummaryStore>();
        services.TryAddScoped<ICatalogDiscoveryWorkPlanningReadPort>(sp => sp.GetRequiredService<RavenCatalogDiscoveryWorkSummaryStore>());
        services.TryAddScoped<IPotentialCatalogLookupWorkStore, RavenPotentialCatalogLookupWorkStore>();
        services.TryAddScoped<ICatalogSearchTrackingStore, RavenCatalogSearchTrackingStore>();
        services.TryAddScoped<IActiveLookupWorkStore, RavenActiveLookupWorkStore>();
        services.TryAddSingleton<IMusicCatalogCandidateSearch, RavenMusicCatalogCandidateSearch>();
        services.TryAddSingleton<ILocalMusicTrackSearch, RavenLocalMusicTrackSearch>();
        return services;
    }
}
