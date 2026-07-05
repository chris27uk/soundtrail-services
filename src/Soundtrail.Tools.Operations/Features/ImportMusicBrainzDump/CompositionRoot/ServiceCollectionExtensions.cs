using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Input;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImportMusicBrainzDumpFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ImportMusicBrainzDumpHandler>();
        services.TryAddScoped<IReadMusicBrainzDumpPort, FileSystemMusicBrainzDumpReader>();
        services.TryAddScoped<MusicBrainzDumpCommandLineRunner>();
        return services;
    }
}
