using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Soundtrail.Adapters.MusicTrackEventStore.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMusicTrackStoredEventTranslations(this IServiceCollection services)
    {
        services.TryAddSingleton<IMusicTrackStoredEventRecordTranslator>(MusicTrackStoredEventRecordTranslator.Default);
        return services;
    }
}
