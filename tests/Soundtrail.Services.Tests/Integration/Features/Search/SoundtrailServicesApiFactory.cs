using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Tests.Integration.Features.Search;

public sealed class SoundtrailServicesApiFactory : WebApplicationFactory<Program>
{
    public ApiFakeQueryCachePort QueryCache { get; } = new();

    public ApiFakeCatalogLookupPort TrackLookup { get; } = new();

    public ApiFakeTrackSearchPort TrackSearch { get; } = new();

    public ApiFakeResolutionDemandPort DemandStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQueryCachePort>();
            services.RemoveAll<ICatalogLookupPort>();
            services.RemoveAll<ITrackSearchPort>();
            services.RemoveAll<IResolutionDemandPort>();

            services.AddSingleton<IQueryCachePort>(QueryCache);
            services.AddSingleton<ICatalogLookupPort>(TrackLookup);
            services.AddSingleton<ITrackSearchPort>(TrackSearch);
            services.AddSingleton<IResolutionDemandPort>(DemandStore);
        });
    }
}

internal static class ApiKnownTracks
{
    public static SearchResult MrBrightside() =>
        new(
            TrackTitle.From("Mr. Brightside"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20400274"),
            Mbid.From("mr-brightside-mbid"),
            AppleId.From("apple-mr-brightside"),
            SpotifyId.From("spotify-mr-brightside"),
            ConfidenceScore.From(0.98));
}
