using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.Worker;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.CompositionRoot;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.EndToEnd.Search;

internal sealed class EndToEndWorkerDependencyProvider(
    string baseUrl,
    LookupExecutionReceiptStoreFake.State receiptState) : IWorkerDependencyProvider
{
    public void AddSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(receiptState);
        services.TryAddSingleton<ILookupExecutionReceiptStore, LookupExecutionReceiptStoreFake>();
    }

    public void AddOnDemandMetadataLookupDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IOptions<MusicBrainzOptions>>(
            Options.Create(
                new MusicBrainzOptions
                {
                    BaseUrl = baseUrl,
                    UserAgent = "Soundtrail.Tests/1.0"
                }));

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
        services.TryAddSingleton<IOptions<OdesliOptions>>(
            Options.Create(
                new OdesliOptions
                {
                    BaseUrl = baseUrl,
                    UserCountry = "US"
                }));

        if (services.All(x => x.ServiceType != typeof(IGetMusicTrackReference)))
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
