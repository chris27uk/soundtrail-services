using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.CompositionRoot;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Model;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Ports;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.Work;
using Soundtrail.Services.Enrichment.CatalogImport.Infrastructure.Lease;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.CatalogImport;

internal sealed class DownloadDumpAndShardTestAdapter(DownloadDumpAndShardPorts ports) : ISociableFeature
{
    public static DownloadDumpAndShardTestAdapter Default() => new(DefaultPorts());

    public static DownloadDumpAndShardTestAdapter With(
        Func<DownloadDumpAndShardPorts, DownloadDumpAndShardPorts> customize) =>
        new(customize(DefaultPorts()));

    public static DownloadDumpAndShardPorts DefaultPorts() =>
        new(
            sp => sp.GetRequiredService<IDownloadDumpAndShardWorkQueue>(),
            sp => ActivatorUtilities.CreateInstance<DownloadDumpAndShardJob>(sp),
            sp => sp.GetRequiredService<IMusicBrainzDumpArchiveStore>(),
            sp => sp.GetRequiredService<IMusicBrainzDumpShardStore>(),
            _ => new ArtistShardPartitioner());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();
        services.TryAddSingleton<IOptions<MusicBrainzDumpOptions>>(_ =>
            Options.Create(new MusicBrainzDumpOptions { ShardCount = 2, LeaseDuration = TimeSpan.FromMinutes(5) }));
        services.TryAddSingleton<ICatalogImportLeaseOwner>(_ => CatalogImportLeaseOwnerFake.Default);
        services.TryAddSingleton<DownloadDumpAndShardWorkQueueFake>();
        services.TryAddSingleton<IDownloadDumpAndShardWorkQueue>(
            sp => sp.GetRequiredService<DownloadDumpAndShardWorkQueueFake>());
        services.TryAddSingleton<MusicBrainzDumpArchiveStoreFake>();
        services.TryAddSingleton<IMusicBrainzDumpArchiveStore>(
            sp => sp.GetRequiredService<MusicBrainzDumpArchiveStoreFake>());
        services.TryAddSingleton<MusicBrainzDumpShardStoreFake>();
        services.TryAddSingleton<IMusicBrainzDumpShardStore>(
            sp => sp.GetRequiredService<MusicBrainzDumpShardStoreFake>());

        DownloadDumpAndShardComposition.Configure(services, ports);
    }
}
