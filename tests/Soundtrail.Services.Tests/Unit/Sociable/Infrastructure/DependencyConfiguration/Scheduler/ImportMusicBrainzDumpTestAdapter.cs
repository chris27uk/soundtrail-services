using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.CompositionRoot;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Ports;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.Scheduler;

internal sealed class ImportMusicBrainzDumpTestAdapter(ImportMusicBrainzDumpPorts ports) : ISociableFeature
{
    public static ImportMusicBrainzDumpTestAdapter Default() => new(DefaultPorts());

    public static ImportMusicBrainzDumpTestAdapter With(
        Func<ImportMusicBrainzDumpPorts, ImportMusicBrainzDumpPorts> customize) =>
        new(customize(DefaultPorts()));

    public static ImportMusicBrainzDumpPorts DefaultPorts() =>
        new(
            sp => sp.GetRequiredService<ICommandBus>(),
            sp => sp.GetRequiredService<IMusicBrainzDumpImportJobStore>(),
            sp => sp.GetRequiredService<IMusicBrainzDumpSnapshotCatalog>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<IOptions<MusicBrainzDumpOptions>>(_ =>
            Options.Create(new MusicBrainzDumpOptions()));
        services.TryAddSingleton<MusicBrainzDumpSnapshotCatalogFake>();
        services.TryAddSingleton<IMusicBrainzDumpSnapshotCatalog>(
            sp => sp.GetRequiredService<MusicBrainzDumpSnapshotCatalogFake>());
        ImportMusicBrainzDumpComposition.Configure(services, ports);
    }
}
