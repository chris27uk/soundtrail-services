using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.SourceBudgets.Configuration;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupAlbumMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupArtistMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupTrackMetadata.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnLookupStreamingLocations.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using StackExchange.Redis;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;

public sealed class ProductionWorkerDependencyProvider : IWorkerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWorkerRavenDocumentStore(configuration);
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection(SourceApiBudgetsOptions.SectionName));
        services.Configure<RedisLookupExecutionAdmissionOptions>(
            configuration.GetSection(RedisLookupExecutionAdmissionOptions.SectionName));
        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(
                configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException("Connection string 'Redis' is required for lookup execution admission.")));
        services.TryAddScoped<ILookupExecutionAdmissionPort, RedisLookupExecutionAdmissionPort>();
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

    public void AddLookupArtistMetadataDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MusicBrainzOptions>(configuration.GetSection(MusicBrainzOptions.SectionName));
        if (!services.Any(x => x.ServiceType == typeof(IGetArtistMetadata)))
        {
            services.AddHttpClient<IGetArtistMetadata, MusicBrainzGetArtistMetadata>()
                .ConfigureHttpClient((sp, httpClient) =>
                {
                    var cfg = sp.GetRequiredService<IOptions<MusicBrainzOptions>>().Value;
                    MusicBrainzGetTrackMetadata.ConfigureHttpClient(httpClient, cfg);
                });
        }
    }

    public void AddLookupAlbumMetadataDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MusicBrainzOptions>(configuration.GetSection(MusicBrainzOptions.SectionName));
        if (!services.Any(x => x.ServiceType == typeof(IGetAlbumMetadata)))
        {
            services.AddHttpClient<IGetAlbumMetadata, MusicBrainzGetAlbumMetadata>()
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
