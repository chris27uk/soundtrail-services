using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog.Browsing;

namespace Soundtrail.Services.Api.Features.ListTracksByArtist.CompositionRoot;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddListTracksByArtistFeature(this IServiceCollection services)
    {
        services.TryAddScoped<IApiHandler<ListTracksByArtistCommand, ArtistTracksResponse?>, ListTracksByArtistHandler>();
        return services;
    }
}
