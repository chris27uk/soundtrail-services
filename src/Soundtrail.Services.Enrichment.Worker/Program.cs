using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.IntegrationMessaging.Responses;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.OnDemandMetadataLookup.Lookup;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.PlaybackReferencesLookupExecution.GetReference;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Wolverine;
using Wolverine.AzureServiceBus;
using Wolverine.RavenDb;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

var serviceBusOptions = builder.Configuration
    .GetSection(ServiceBusOptions.SectionName)
    .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

builder.UseWolverine(opts =>
{
    opts.Discovery.DisableConventionalDiscovery();
    opts.Discovery.IncludeType<MusicBrainzLookupExecutionListener>();
    opts.Discovery.IncludeType<PlaybackReferencesLookupExecutionListener>();
    opts.UseRavenDbPersistence();
    opts.Policies.AutoApplyTransactions();

    opts.UseAzureServiceBus(serviceBusOptions.ConnectionString)
        .AutoProvision()
        .EnableWolverineControlQueues();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName)
        .ProcessInline();

    opts.ListenToAzureServiceBusQueue(serviceBusOptions.PlaybackReferencesLookupQueueName)
        .ProcessInline();

    opts.PublishMessage<EnrichmentResponseDto>()
        .ToAzureServiceBusQueue(serviceBusOptions.EnrichmentResponsesQueueName);
});

builder.Services.AddWorkerRavenDocumentStore(builder.Configuration);
builder.Services.Configure<MusicBrainzOptions>(builder.Configuration.GetSection(MusicBrainzOptions.SectionName));
builder.Services.Configure<OdesliOptions>(builder.Configuration.GetSection(OdesliOptions.SectionName));
builder.Services.AddHttpClient<IGetCanonicalMusicMetadata, MusicBrainzGetCanonicalMusicMetadata>()
    .ConfigureHttpClient((sp, httpClient) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MusicBrainzOptions>>().Value;
        MusicBrainzGetCanonicalMusicMetadata.ConfigureHttpClient(httpClient, options);
    });
builder.Services.AddHttpClient<IGetMusicTrackReference, OdesliStreamingReferences>()
    .ConfigureHttpClient((sp, httpClient) =>
    {
        var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<OdesliOptions>>().Value;
        OdesliStreamingReferences.ConfigureHttpClient(httpClient, options);
    });
builder.Services.AddScoped<OnDemandLookupMetadataHandler>();
builder.Services.AddScoped<ExecutePlaybackReferencesLookupHandler>();
builder.Services.AddScoped<MusicBrainzLookupExecutionListener>();
builder.Services.AddScoped<PlaybackReferencesLookupExecutionListener>();

var host = builder.Build();

await host.RunAsync();
