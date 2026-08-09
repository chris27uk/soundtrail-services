using Microsoft.Extensions.Configuration;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.CompositionRoot;

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
            sp => sp.GetRequiredService<IMusicBrainzDumpImportJobStore>());

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration) =>
        ImportMusicBrainzDumpComposition.Configure(services, ports);
}
