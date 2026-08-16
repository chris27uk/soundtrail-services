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
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.CompositionRoot;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Adapters;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Messaging;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.CompositionRoot;

[Autodiscover]
public sealed class ImportCatalogShardFeature : IFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAzureServiceBusCommandBus();
        services.AddAzureServiceBusListener<ImportMusicBrainzDumpShardCommandDto, ImportMusicBrainzDumpShard>();
        services.AddRavenDocumentStore(configuration);
        services.AddMusicBrainzDumpImportJobStore();
        services.AddArtistCatalogEventStreamRepository();
        services.TryAddSingleton<ITypeRegistry>(_ => TypeTranslationRegistry.Default);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));
        services.Configure<MusicBrainzDumpOptions>(configuration.GetSection(MusicBrainzDumpOptions.SectionName));
        services.TryAddSingleton<ICatalogImportLeaseOwner, CatalogImportLeaseOwner>();
        services.TryAddSingleton<IImportCatalogShardWorkQueue, ChannelImportCatalogShardWorkQueue>();
        services.TryAddSingleton<IDownloadDumpAndShardWorkQueue, ChannelDownloadDumpAndShardWorkQueue>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, ImportCatalogShardWorkPump>());

        if (MusicBrainzDumpStorageRegistration.UsesBlobStorage(configuration))
        {
            MusicBrainzDumpStorageRegistration.AddMusicBrainzDumpBlobInfrastructure(services, configuration);
        }

        ImportCatalogShardComposition.Configure(
            services,
            new(
                sp => sp.GetRequiredService<IImportCatalogShardWorkQueue>(),
                sp => ActivatorUtilities.CreateInstance<ImportCatalogShardJob>(sp),
                _ => new MusicBrainzArtistDumpRowMapper(),
                _ => new MusicBrainzReleaseGroupDumpRowMapper(),
                _ => new MusicBrainzTrackDumpRowMapper(),
                sp => ActivatorUtilities.CreateInstance<CatalogDumpBatchWriter>(sp),
                MusicBrainzDumpStorageRegistration.CreateShardStoreFactory(configuration),
                sp => sp.GetRequiredService<IDownloadDumpAndShardWorkQueue>()));
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
