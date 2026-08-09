using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.DownloadDumpAndShard.CompositionRoot;
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
            sp => ActivatorUtilities.CreateInstance<DownloadDumpAndShardJob>(sp));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();
        services.TryAddSingleton<ICatalogImportLeaseOwner>(_ => CatalogImportLeaseOwnerFake.Default);
        services.TryAddSingleton<DownloadDumpAndShardWorkQueueFake>();
        services.TryAddSingleton<IDownloadDumpAndShardWorkQueue>(
            sp => sp.GetRequiredService<DownloadDumpAndShardWorkQueueFake>());

        DownloadDumpAndShardComposition.Configure(services, ports);
    }
}
