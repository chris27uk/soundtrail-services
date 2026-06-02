using Soundtrail.Services.Api.Features.Health;
using Soundtrail.Services.Api.Features.Search;
using Soundtrail.Services.Api.Infrastructure.Messaging;
using Soundtrail.Services.Api.Infrastructure.Search;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Api.Infrastructure.TableStorage;
using Soundtrail.Services.Api.Infrastructure.Time;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Search.Queueing;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Shared;
using Wolverine;
using Wolverine.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IClockPort, SystemClock>();

if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddSingleton<IQueryCachePort, AzureTableQueryCache>();
    builder.Services.AddSingleton<IResolutionDemandPort, AzureTableResolutionDemandStore>();
    builder.Services.AddSingleton<IResolutionDemandSignalPort, InMemoryResolutionDemandSignalQueue>();
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
    builder.Services.AddSingleton(sp =>
    {
        var lookup = new AzureTableTrackLookup();
        lookup.Seed(
            new Track(
                TrackTitle.From("Mr. Brightside"),
                ArtistName.From("The Killers"),
                Isrc.From("USIR20400274"),
                Mbid.From("mr-brightside-mbid"),
                AppleId.From("apple-mr-brightside"),
                SpotifyId.From("spotify-mr-brightside"),
                DurationMs.From(222000)));

        return lookup;
    });
    builder.Services.AddSingleton<ICatalogLookupPort>(sp => sp.GetRequiredService<AzureTableTrackLookup>());
}
else
{
    builder.Host.UseWolverine(opts =>
    {
        var options = builder.Configuration
            .GetSection(ServiceBusOptions.SectionName)
            .Get<ServiceBusOptions>() ?? throw new InvalidOperationException("ServiceBus configuration is required.");

        opts.UseAzureServiceBus(options.ConnectionString)
            .AutoProvision()
            .SystemQueuesAreEnabled(false);

        opts.PublishMessage<ResolutionDemandSignal>()
            .ToAzureServiceBusQueue(options.LookupMusicRequestsQueueName);
    });

    builder.Services.AddRavenDocumentStore(builder.Configuration);
    builder.Services.AddLookupMusicRequestQueue(builder.Configuration);
    builder.Services.AddSingleton<IQueryCachePort, RavenQueryCache>();
    builder.Services.AddSingleton<IResolutionDemandPort, RavenResolutionDemandStore>();
    builder.Services.AddSingleton<ITrackSearchPort, RavenTrackSearchIndex>();
    builder.Services.AddSingleton<ICatalogLookupPort, RavenTrackLookup>();
}

builder.Services.AddSingleton<SearchMusicHandler>();

var app = builder.Build();

app.MapHealthEndpoints();
app.MapSearchEndpoints();

app.Run();
