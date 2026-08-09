using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.CompositionRoot;

public sealed record DownloadDumpAndShardPorts(
    Func<IServiceProvider, IDownloadDumpAndShardWorkQueue> WorkQueue,
    Func<IServiceProvider, IDownloadDumpAndShardJob> DownloadDumpAndShardJob);

public static class DownloadDumpAndShardComposition
{
    public static void Configure(IServiceCollection services, DownloadDumpAndShardPorts ports)
    {
        services.TryAddSingleton(ports.WorkQueue);
        services.TryAddSingleton(ports.DownloadDumpAndShardJob);
        services.TryAddScoped<IHandler<StartMusicBrainzDumpImport>, StartMusicBrainzDumpImportHandler>();
    }
}
