using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Adapters.FeatureOrchestration;
using Soundtrail.Adapters.Persistence;
using Soundtrail.Adapters.Persistence.MusicBrainzDumpImport;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.MusicBrainzDumpImport;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Adapters;
using Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.Ports;
using Soundtrail.Services.Enrichment.Scheduler.Infrastructure;

namespace Soundtrail.Services.Enrichment.Scheduler.Features.ImportMusicBrainzDump.CompositionRoot;

[Autodiscover]
public sealed class ImportMusicBrainzDumpFeature : ISchedulerFeature
{
    public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddRavenDocumentStore(configuration);
        services.AddMusicBrainzDumpImportJobStore();
        services.Configure<MusicBrainzDumpOptions>(configuration.GetSection(MusicBrainzDumpOptions.SectionName));
        services.AddHttpClient<IMusicBrainzDumpSnapshotCatalog, HttpMusicBrainzDumpSnapshotCatalog>();

        ImportMusicBrainzDumpComposition.Configure(
            services,
            new(
                sp => sp.GetRequiredService<ICommandBus>(),
                sp => sp.GetRequiredService<IMusicBrainzDumpImportJobStore>(),
                sp => sp.GetRequiredService<IMusicBrainzDumpSnapshotCatalog>()));
        services.TryAddScoped<ImportMusicBrainzDumpTickerFunctions>();
    }

    public void ConfigureApplication(WebApplication app)
    {
    }
}
