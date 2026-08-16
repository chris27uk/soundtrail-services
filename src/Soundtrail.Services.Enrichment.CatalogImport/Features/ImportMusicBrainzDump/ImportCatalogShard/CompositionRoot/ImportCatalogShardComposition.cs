using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.CompositionRoot;

public sealed record ImportCatalogShardPorts(
    Func<IServiceProvider, IImportCatalogShardWorkQueue> WorkQueue,
    Func<IServiceProvider, IImportCatalogShardJob> ImportCatalogShardJob,
    Func<IServiceProvider, IMusicBrainzArtistDumpRowMapper> ArtistRowMapper,
    Func<IServiceProvider, IMusicBrainzReleaseGroupDumpRowMapper> ReleaseGroupRowMapper,
    Func<IServiceProvider, IMusicBrainzTrackDumpRowMapper> TrackRowMapper,
    Func<IServiceProvider, ICatalogDumpBatchWriter> BatchWriter,
    Func<IServiceProvider, IMusicBrainzDumpShardStore> ShardStore,
    Func<IServiceProvider, IDownloadDumpAndShardWorkQueue> DownloadWorkQueue);

public static class ImportCatalogShardComposition
{
    public static void Configure(IServiceCollection services, ImportCatalogShardPorts ports)
    {
        services.TryAddScoped(ports.WorkQueue);
        services.TryAddScoped(ports.ImportCatalogShardJob);
        services.TryAddSingleton(ports.ArtistRowMapper);
        services.TryAddSingleton(ports.ReleaseGroupRowMapper);
        services.TryAddSingleton(ports.TrackRowMapper);
        services.TryAddSingleton(ports.BatchWriter);
        services.TryAddSingleton(ports.ShardStore);
        services.TryAddSingleton(ports.DownloadWorkQueue);
        services.TryAddScoped<IHandler<ImportMusicBrainzDumpShard>, ImportMusicBrainzDumpShardHandler>();
    }
}
