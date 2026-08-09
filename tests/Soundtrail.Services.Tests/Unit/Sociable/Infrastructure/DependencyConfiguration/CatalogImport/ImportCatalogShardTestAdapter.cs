using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard;
using Soundtrail.Services.Enrichment.CatalogImport.Features.ImportMusicBrainzDump.ImportCatalogShard.CompositionRoot;
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
            sp => ActivatorUtilities.CreateInstance<ImportCatalogShardJob>(sp));

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddLogging();
        services.TryAddSingleton<ICatalogImportLeaseOwner>(_ => CatalogImportLeaseOwnerFake.Default);
        services.TryAddSingleton<ImportCatalogShardWorkQueueFake>();
        services.TryAddSingleton<IImportCatalogShardWorkQueue>(
            sp => sp.GetRequiredService<ImportCatalogShardWorkQueueFake>());

        ImportCatalogShardComposition.Configure(services, ports);
    }
}
