using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Catalog.Projector.Features.ProjectMusicTrackCatalog;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.Adapters;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddImportMusicBrainzDumpFeature(this IServiceCollection services)
    {
        services.TryAddScoped<ImportMusicBrainzDumpHandler>();
        services.TryAddScoped<ProjectMusicTrackCatalogHandler>();
        services.TryAddScoped<IReadMusicBrainzDumpPort, FileSystemMusicBrainzDumpReader>();
        services.TryAddScoped<MusicBrainzDumpCommandLineRunner>();
        return services;
    }
}
