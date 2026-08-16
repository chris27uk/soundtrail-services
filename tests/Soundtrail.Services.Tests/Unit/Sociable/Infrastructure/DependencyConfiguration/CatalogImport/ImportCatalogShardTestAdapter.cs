using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.CompositionRoot;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.CatalogImport;

internal sealed class ImportCatalogShardTestAdapter(ImportCatalogShardPorts ports) : ISociableFeature
{
    public static ImportCatalogShardTestAdapter Default() => new(DefaultPorts());

    public static ImportCatalogShardTestAdapter With(
        Func<ImportCatalogShardPorts, ImportCatalogShardPorts> customize) =>
        new(customize(DefaultPorts()));

    public static ImportCatalogShardPorts DefaultPorts() =>
        new(
            sp => sp.GetRequiredService<IImportCatalogShardWorkQueue>(),
            sp => ActivatorUtilities.CreateInstance<ImportCatalogShardJob>(sp),
            _ => new MusicBrainzArtistDumpRowMapper(),
            sp => sp.GetRequiredService<ICatalogArtistImportWriter>(),
            sp => sp.GetRequiredService<IMusicBrainzDumpShardStore>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();
        services.TryAddSingleton<IOptions<MusicBrainzDumpOptions>>(_ =>
            Options.Create(new MusicBrainzDumpOptions { ShardCount = 2, LeaseDuration = TimeSpan.FromMinutes(5) }));
        services.TryAddSingleton<ICatalogImportLeaseOwner>(_ => CatalogImportLeaseOwnerFake.Default);
        services.TryAddSingleton<ImportCatalogShardWorkQueueFake>();
        services.TryAddSingleton<IImportCatalogShardWorkQueue>(
            sp => sp.GetRequiredService<ImportCatalogShardWorkQueueFake>());
        services.TryAddSingleton<MusicBrainzDumpShardStoreFake>();
        services.TryAddSingleton<IMusicBrainzDumpShardStore>(
            sp => sp.GetRequiredService<MusicBrainzDumpShardStoreFake>());
        services.TryAddSingleton<CatalogArtistImportWriterFake>();
        services.TryAddSingleton<ICatalogArtistImportWriter>(
            sp => sp.GetRequiredService<CatalogArtistImportWriterFake>());

        ImportCatalogShardComposition.Configure(services, ports);
    }
}
