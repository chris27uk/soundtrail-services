using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;

namespace Soundtrail.Adapters.Persistence.MusicBrainzDumpImport;

public static class MusicBrainzDumpImportJobStoreServiceCollectionExtensions
{
    public static IServiceCollection AddMusicBrainzDumpImportJobStore(this IServiceCollection services)
    {
        services.TryAddScoped<IMusicBrainzDumpImportJobStore, RavenMusicBrainzDumpImportJobStore>();
        return services;
    }
}
