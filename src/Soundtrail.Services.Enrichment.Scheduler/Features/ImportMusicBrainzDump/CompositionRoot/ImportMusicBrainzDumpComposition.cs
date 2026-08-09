using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Domain.Operations;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.CompositionRoot;

public sealed record ImportMusicBrainzDumpPorts(
    Func<IServiceProvider, ICommandBus> CommandBus,
    Func<IServiceProvider, IMusicBrainzDumpImportJobStore> JobStore);

public static class ImportMusicBrainzDumpComposition
{
    public static void Configure(IServiceCollection services, ImportMusicBrainzDumpPorts ports)
    {
        services.TryAddScoped(ports.CommandBus);
        services.TryAddScoped(ports.JobStore);
        services.TryAddScoped<IScheduledMessageHandler<ImportMusicBrainzDumpCommand>, ImportMusicBrainzDumpHandler>();
    }
}
