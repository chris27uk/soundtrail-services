using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Timing;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Idempotency.Storage;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Messaging;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.MusicMetadata;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;
using Soundtrail.Services.Enrichment.Worker.Shared.ExecutionAdmission;
using Soundtrail.Services.Enrichment.Worker.Shared.MusicMetadata;
using Soundtrail.Services.ServiceDefaults;
using StackExchange.Redis;
using Wolverine;
using Wolverine.AzureServiceBus;
using WebApplication = Microsoft.AspNetCore.Builder.WebApplication;
using DomainCommandBus = Soundtrail.Domain.Abstractions.ICommandBus;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks.Composition;

[Autodiscover]
public sealed class LookupMusicbrainzAlbumTracksFeature : IFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddWolverineCommandBus();
        services.AddWorkerRavenDocumentStore(configuration);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<RedisLookupExecutionAdmissionOptions>(configuration.GetSection(RedisLookupExecutionAdmissionOptions.SectionName));
        services.Configure<SourceApiBudgetsOptions>(configuration.GetSection("SourceBudgets"));
        services.Configure<MusicBrainzOptions>(configuration.GetSection(MusicBrainzOptions.SectionName));
        services.AddHttpClient(MusicbrainzCatalogBrowsePort.HttpClientName, (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MusicBrainzOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        });

        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis") ?? throw new InvalidOperationException("Redis connection string is required.")));
        services.TryAddSingleton<IClockPort, SystemClockPort>();
        services.AddKeyedScoped<IHandler<LookupMusicbrainzAlbumTracksMessage>>(
            "business",
            (sp, _) => new LookupMusicbrainzAlbumTracksHandler(
                sp.GetRequiredService<IReadTracksByAlbumIdPort>(),
                sp.GetRequiredService<IClockPort>(),
                sp.GetRequiredService<DomainCommandBus>()));
        services.AddKeyedScoped<IHandler<LookupMusicbrainzAlbumTracksMessage>>(
            "admitted",
            (sp, _) => new AdmittedLookupMusicbrainzAlbumTracksHandlerDecorator(
                sp.GetRequiredKeyedService<IHandler<LookupMusicbrainzAlbumTracksMessage>>("business"),
                sp.GetRequiredService<DomainCommandBus>(),
                sp.GetRequiredService<ILookupExecutionAdmissionPort>(),
                sp.GetRequiredService<IClockPort>()));
        services.AddKeyedScoped<IHandler<LookupMusicbrainzAlbumTracksMessage>>(
            "idempotent",
            (sp, _) => new IdempotentLookupMusicbrainzAlbumTracksHandlerDecorator(
                sp.GetRequiredKeyedService<IHandler<LookupMusicbrainzAlbumTracksMessage>>("admitted"),
                sp.GetRequiredService<ILookupExecutionReceiptStore>(),
                sp.GetRequiredService<DomainCommandBus>(),
                sp.GetRequiredService<IClockPort>()));
        services.TryAddScoped<IHandler<LookupMusicbrainzAlbumTracksMessage>>(sp =>
            sp.GetRequiredKeyedService<IHandler<LookupMusicbrainzAlbumTracksMessage>>("idempotent"));
        services.TryAddScoped<IReadTracksByAlbumIdPort>(
            sp => new MusicbrainzCatalogBrowsePort(
                sp.GetRequiredService<IHttpClientFactory>().CreateClient(MusicbrainzCatalogBrowsePort.HttpClientName),
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MusicBrainzOptions>>()));
        services.TryAddScoped<ILookupExecutionAdmissionPort, RedisLookupExecutionAdmissionPort>();
        services.TryAddScoped<ILookupExecutionReceiptStore, RavenLookupExecutionReceiptStore>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }

    public void ConfigureMessaging(WolverineOptions options, IConfiguration configuration, IHostEnvironment environment)
    {
        options.UseRuntimeCompilation();
        var serviceBusOptions = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
            ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        if (environment.IsEnvironment("Testing"))
        {
            options.StubAllExternalTransports();
            return;
        }

        var transport = options.UseAzureServiceBus(serviceBusOptions.ConnectionString).SystemQueuesAreEnabled(false);
        if (!serviceBusOptions.ConnectionString.IsDevelopmentEmulatorConnectionString())
        {
            transport.AutoProvision();
        }

        options.ListenToAzureServiceBusQueue(serviceBusOptions.MusicBrainzLookupQueueName).ProcessInline();
        options.PublishMessage<CatalogLookupCompleted>().ToAzureServiceBusQueue(serviceBusOptions.CatalogLookupCompletedQueueName);
    }
}
