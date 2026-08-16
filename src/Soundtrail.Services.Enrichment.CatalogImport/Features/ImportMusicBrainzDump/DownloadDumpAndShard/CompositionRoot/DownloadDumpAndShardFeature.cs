using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Soundtrail.Contracts.IntegrationMessaging.Commands;
using Soundtrail.Adapters.EventSourcing.CompositionRoot;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Messaging;
using Soundtrail.Adapters.Messaging.Asb;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Persistence.MusicBrainzDumpImport;
using Soundtrail.Adapters.TypeRegistry;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Messaging;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.CompositionRoot;

[Autodiscover]
public sealed class DownloadDumpAndShardFeature : IFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureServiceBusCommandBus();
        services.AddAzureServiceBusListener<StartMusicBrainzDumpImportCommandDto, StartMusicBrainzDumpImport>();
        services.AddRavenDocumentStore(configuration);
        services.AddMusicBrainzDumpImportJobStore();
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<MusicBrainzDumpOptions>(configuration.GetSection(MusicBrainzDumpOptions.SectionName));
        services.TryAddSingleton<ICatalogImportLeaseOwner, CatalogImportLeaseOwner>();
        services.TryAddSingleton<IDownloadDumpAndShardWorkQueue, ChannelDownloadDumpAndShardWorkQueue>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, DownloadDumpAndShardWorkPump>());
        services.AddHttpClient<IMusicBrainzDumpDownloader, HttpMusicBrainzDumpDownloader>();
        services.TryAddSingleton<IMusicBrainzDumpTarXzExtractor, MusicBrainzDumpTarXzExtractor>();

        DownloadDumpAndShardComposition.Configure(
            services,
            new(
                sp => sp.GetRequiredService<IDownloadDumpAndShardWorkQueue>(),
                sp => ActivatorUtilities.CreateInstance<DownloadDumpAndShardJob>(sp),
                sp => ActivatorUtilities.CreateInstance<LocalMusicBrainzDumpArchiveStore>(sp),
                sp => ActivatorUtilities.CreateInstance<LocalMusicBrainzDumpShardStore>(sp),
                _ => new ArtistShardPartitioner()));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
