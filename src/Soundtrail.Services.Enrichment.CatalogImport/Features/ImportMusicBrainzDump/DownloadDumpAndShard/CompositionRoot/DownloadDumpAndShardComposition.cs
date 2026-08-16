using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.CompositionRoot;

public sealed record DownloadDumpAndShardPorts(
    Func<IServiceProvider, IDownloadDumpAndShardWorkQueue> WorkQueue,
    Func<IServiceProvider, IDownloadDumpAndShardJob> DownloadDumpAndShardJob,
    Func<IServiceProvider, IMusicBrainzDumpArchiveStore> ArchiveStore,
    Func<IServiceProvider, IMusicBrainzDumpShardStore> ShardStore,
    Func<IServiceProvider, IArtistShardPartitioner> Partitioner);

public static class DownloadDumpAndShardComposition
{
    public static void Configure(IServiceCollection services, DownloadDumpAndShardPorts ports)
    {
        services.TryAddScoped(ports.WorkQueue);
        services.TryAddScoped(ports.DownloadDumpAndShardJob);
        services.TryAddSingleton(ports.ArchiveStore);
        services.TryAddSingleton(ports.ShardStore);
        services.TryAddSingleton(ports.Partitioner);
        services.TryAddScoped<IHandler<StartMusicBrainzDumpImport>, StartMusicBrainzDumpImportHandler>();
    }
}
