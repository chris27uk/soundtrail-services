using MusicResolver.Api.Endpoints;
using MusicResolver.Application.Ports;
using MusicResolver.Application.Search;
using MusicResolver.Domain.Tracks;
using MusicResolver.Domain.ValueTypes;
using MusicResolver.Infrastructure.Search;
using MusicResolver.Infrastructure.TableStorage;
using MusicResolver.Infrastructure.Time;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IQueryCachePort, AzureTableQueryCache>();
builder.Services.AddSingleton<ITrackLookupPort, AzureTableTrackLookup>();
builder.Services.AddSingleton<IResolutionDemandPort, AzureTableResolutionDemandStore>();
builder.Services.AddSingleton<IClockPort, SystemClock>();
builder.Services.AddSingleton<SqliteTrackSearchIndex>(sp =>
{
    var index = new SqliteTrackSearchIndex();
    index.Seed(
        new SearchResult(
            TrackTitle.From("Mr. Brightside"),
            ArtistName.From("The Killers"),
            Isrc.From("USIR20400274"),
            Mbid.From("mr-brightside-mbid"),
            AppleId.From("apple-mr-brightside"),
            SpotifyId.From("spotify-mr-brightside"),
            ConfidenceScore.From(0.98)));

    return index;
});
builder.Services.AddSingleton<ITrackSearchPort>(sp => sp.GetRequiredService<SqliteTrackSearchIndex>());
builder.Services.AddSingleton<SearchMusicHandler>();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapSearchEndpoints();
app.MapResolveEndpoints();

app.Run();

public partial class Program
{
}
