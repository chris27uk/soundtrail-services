using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport.Messages;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;

namespace Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.CompositionRoot;

public sealed record ImportCatalogShardPorts(
    Func<IServiceProvider, IImportCatalogShardWorkQueue> WorkQueue,
    Func<IServiceProvider, IImportCatalogShardJob> ImportCatalogShardJob);

public static class ImportCatalogShardComposition
{
    public static void Configure(IServiceCollection services, ImportCatalogShardPorts ports)
    {
        services.TryAddScoped(ports.WorkQueue);
        services.TryAddScoped(ports.ImportCatalogShardJob);
        services.TryAddScoped<IHandler<ImportMusicBrainzDumpShard>, ImportMusicBrainzDumpShardHandler>();
    }
}
