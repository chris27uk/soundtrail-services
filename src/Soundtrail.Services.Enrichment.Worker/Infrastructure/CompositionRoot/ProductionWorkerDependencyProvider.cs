using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Adapters;
using Soundtrail.Services.Enrichment.DiscoveryPlanner.Shared.SourceBudgets.Configuration;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;

public sealed class ProductionWorkerDependencyProvider : IWorkerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWorkerRavenDocumentStore(configuration);
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection(SourceApiBudgetsOptions.SectionName));
        services.TryAddScoped<IReserveSourceApiBudgetPort, RavenCompareExchangeSourceApiBudgetPort>();
    }

    public void AddOnDemandMetadataLookupDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MusicBrainzOptions>(configuration.GetSection(MusicBrainzOptions.SectionName));
        if (!services.Any(x => x.ServiceType == typeof(IGetCanonicalMusicMetadata)))
        {
            services.AddHttpClient<IGetCanonicalMusicMetadata, MusicBrainzGetCanonicalMusicMetadata>()
                .ConfigureHttpClient((sp, httpClient) =>
                {
                    var cfg = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MusicBrainzOptions>>().Value;
                    MusicBrainzGetCanonicalMusicMetadata.ConfigureHttpClient(httpClient, cfg);
                });
        }
    }

    public void AddPlaybackReferencesLookupDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OdesliOptions>(configuration.GetSection(OdesliOptions.SectionName));
        if (!services.Any(x => x.ServiceType == typeof(IGetMusicTrackReference)))
        {
            services.AddHttpClient<IGetMusicTrackReference, OdesliStreamingReferences>()
                .ConfigureHttpClient((sp, httpClient) =>
                {
                    var cfg = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OdesliOptions>>().Value;
                    OdesliStreamingReferences.ConfigureHttpClient(httpClient, cfg);
                });
        }
    }
}
