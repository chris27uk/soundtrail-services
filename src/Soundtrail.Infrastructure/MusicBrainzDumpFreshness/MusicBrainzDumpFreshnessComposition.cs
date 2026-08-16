using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Raven.Client.Documents;

namespace Soundtrail.Adapters.MusicBrainzDumpFreshness;

public static class MusicBrainzDumpFreshnessComposition
{
    public static IServiceCollection AddMusicBrainzDumpFreshness(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MusicBrainzDumpFreshnessOptions>(
            configuration.GetSection(MusicBrainzDumpFreshnessOptions.SectionName));
        services.TryAddScoped<IMusicBrainzDumpFreshnessEvaluator>(sp =>
            new RavenMusicBrainzDumpFreshnessEvaluator(
                sp.GetRequiredService<IDocumentStore>(),
                sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<MusicBrainzDumpFreshnessOptions>>()));
        return services;
    }
}
