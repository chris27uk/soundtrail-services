using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Soundtrail.Tools.MusicBrainzImport.Features.ImportMusicBrainzDump.CompositionRoot;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.CompositionRoot;
using Soundtrail.Tools.MusicBrainzImport.Infrastructure.Raven;

namespace Soundtrail.Tools.MusicBrainzImport.Infrastructure.CompositionRoot;

public static class AppServiceCollectionExtensions
{
    public static IServiceCollection AddMusicBrainzImportAppServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddMusicBrainzImportRaven(configuration);
        services.AddImportMusicBrainzDumpFeature();
        services.AddReplayCatalogProjectionFeature();
        return services;
    }
}
