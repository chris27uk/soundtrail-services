using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Messaging.Asb;
using Soundtrail.Adapters.Timing;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Adapters;
using Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Ports;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.PlaylistLookups;
using Soundtrail.Services.ServiceDefaults;
using StackExchange.Redis;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using DomainCommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks.Composition;

[Autodiscover]
public sealed class LookupPlaylistTracksFeature : IFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureServiceBusCommandBus();
        services.AddAzureServiceBusListener<PlaylistTracksLookupCommandDto, PlaylistTracksLookupCommandDto>(
            "lookup-music-playlists");
        services.AddWorkerRavenDocumentStore(configuration);
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<RedisLookupExecutionAdmissionOptions>(configuration.GetSection(RedisLookupExecutionAdmissionOptions.SectionName));
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection("SourceBudgets"));
        services.Configure<KworbOptions>(configuration.GetSection(KworbOptions.SectionName));
        services.AddHttpClient(KworbPlaylistTracksPort.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KworbOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Soundtrail/1.0");
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is required.")));
        services.TryAddSingleton<IClockPort, SystemClockPort>();
        services.AddLookupHandlerPipeline<LookupPlaylistTracksByProviderMessage, LookupPlaylistTracksByProviderDecoratorMetadata>(
            sp => new LookupPlaylistTracksByProviderHandler(
                sp.GetRequiredService<IReadPlaylistTracksByProviderPort>(),
                sp.GetRequiredService<IClockPort>(),
                sp.GetRequiredService<DomainCommandBus>()));
        services.TryAddScoped<IReadPlaylistTracksByProviderPort>(
            sp => new KworbPlaylistTracksPort(sp.GetRequiredService<IHttpClientFactory>().CreateClient(KworbPlaylistTracksPort.HttpClientName)));
        services.TryAddScoped<ILookupExecutionAdmissionPort, RedisLookupExecutionAdmissionPort>();
        services.TryAddScoped<ILookupExecutionReceiptStore, RavenLookupExecutionReceiptStore>();
        services.TryAddScoped<IHandler<PlaylistTracksLookupCommandDto>, PlaylistTracksLookupCommandHandler>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
