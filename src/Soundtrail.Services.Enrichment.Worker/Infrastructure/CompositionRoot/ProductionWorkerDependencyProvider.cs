using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Adapters;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Configuration;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupMusicMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;

public sealed class ProductionWorkerDependencyProvider : IWorkerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWorkerRavenDocumentStore(configuration);
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection(SourceApiBudgetsOptions.SectionName));
        services.TryAddScoped<ITryReserveSourceApiBudgetWindowPort, RavenCompareExchangeSourceApiBudgetPort>();
        services.TryAddScoped<IReserveSourceApiBudgetPort, SourceApiBudgetReservationService>();
        services.TryAddScoped<ILookupExecutionAdmissionPort, LegacyLookupExecutionAdmissionPort>();
    }

    public void AddLookupTrackMetadataDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MusicBrainzOptions>(configuration.GetSection(MusicBrainzOptions.SectionName));
        if (!services.Any(x => x.ServiceType == typeof(IGetTrackMetadata)))
        {
            services.AddHttpClient<IGetTrackMetadata, MusicBrainzGetTrackMetadata>()
                .ConfigureHttpClient((sp, httpClient) =>
                {
                    var cfg = sp.GetRequiredService<IOptions<MusicBrainzOptions>>().Value;
                    MusicBrainzGetTrackMetadata.ConfigureHttpClient(httpClient, cfg);
                });
        }
    }

    public void AddLookupStreamingLocationsDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<OdesliOptions>(configuration.GetSection(OdesliOptions.SectionName));
        if (!services.Any(x => x.ServiceType == typeof(IGetMusicTrackReference)))
        {
            services.AddHttpClient<IGetMusicTrackReference, OdesliStreamingReferences>()
                .ConfigureHttpClient((sp, httpClient) =>
                {
                    var cfg = sp.GetRequiredService<IOptions<OdesliOptions>>().Value;
                    OdesliStreamingReferences.ConfigureHttpClient(httpClient, cfg);
                });
        }
    }
}
