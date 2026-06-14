using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain;
using Soundtrail.Services.Api.Features.Search.TrackSearch;

namespace Soundtrail.Services.Api.Features.Search.SearchMusic.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSearchMusicFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IHandler<SearchMusicRequest, SearchMusicResponse>, SearchMusicHandler>();
        return services;
    }
}
