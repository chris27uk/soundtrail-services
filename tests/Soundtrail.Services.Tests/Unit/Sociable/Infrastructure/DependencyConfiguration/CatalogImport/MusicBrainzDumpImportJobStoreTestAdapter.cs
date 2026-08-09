using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.Fakes;

namespace Soundtrail.Services.Tests.Unit.Sociable.Infrastructure.DependencyConfiguration.CatalogImport;

internal sealed class MusicBrainzDumpImportJobStoreTestAdapter : ISociableFeature
{
    public static MusicBrainzDumpImportJobStoreTestAdapter Default() => new();

    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.TryAddSingleton<MusicBrainzDumpImportJobStoreFake>();
        services.TryAddSingleton<IMusicBrainzDumpImportJobStore>(
            sp => sp.GetRequiredService<MusicBrainzDumpImportJobStoreFake>());
    }
}
