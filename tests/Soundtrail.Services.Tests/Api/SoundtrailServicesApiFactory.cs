using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Soundtrail.Services.Application.Ports;
using Soundtrail.Services.Domain.Tracks;
using Soundtrail.Services.Domain.ValueTypes;

namespace Soundtrail.Services.Tests.Api;

public sealed class SoundtrailServicesApiFactory : WebApplicationFactory<Program>
{
    public FakeQueryCachePort QueryCache { get; } = new();

    public FakeTrackLookupPort TrackLookup { get; } = new();

    public FakeTrackSearchPort TrackSearch { get; } = new();

    public FakeResolutionDemandPort DemandStore { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IQueryCachePort>();
            services.RemoveAll<ITrackLookupPort>();
            services.RemoveAll<ITrackSearchPort>();
            services.RemoveAll<IResolutionDemandPort>();

            services.AddSingleton<IQueryCachePort>(QueryCache);
            services.AddSingleton<ITrackLookupPort>(TrackLookup);
            services.AddSingleton<ITrackSearchPort>(TrackSearch);
            services.AddSingleton<IResolutionDemandPort>(DemandStore);
        });
    }
}

public sealed class FakeQueryCachePort : IQueryCachePort
{
    private readonly Dictionary<string, Soundtrail.Services.Application.Search.SearchMusicResponse> _responses = new();

    public Task<Soundtrail.Services.Application.Search.SearchMusicResponse?> GetAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        _responses.TryGetValue(query.Value, out var response);
        return Task.FromResult(response);
    }

    public Task StoreAsync(
        NormalizedSearchQuery query,
        Soundtrail.Services.Application.Search.SearchMusicResponse response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        _responses[query.Value] = response;
        return Task.CompletedTask;
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

public sealed class FakeTrackLookupPort : ITrackLookupPort
{
    public bool Ready { get; set; } = true;

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(Ready);
}

public sealed class FakeTrackSearchPort : ITrackSearchPort
{
    private readonly List<SearchResult> _results = new();

    public bool Ready { get; set; } = true;

    public void Seed(params SearchResult[] results)
    {
        _results.Clear();
        _results.AddRange(results);
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken)
    {
        var results = _results
            .Where(track => NormalizedSearchQuery.FromText($"{track.Title.Value} {track.Artist.Value}")
                .Value.Contains(query.Value, StringComparison.Ordinal))
            .Take(limit.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(Ready);
}

public sealed class FakeResolutionDemandPort : IResolutionDemandPort
{
    private readonly Dictionary<string, QueryId> _queries = new();

    public IReadOnlyCollection<string> RecordedQueries => _queries.Keys.ToArray();

    public Task<QueryId> RecordDemandAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        if (!_queries.TryGetValue(query.Value, out var queryId))
        {
            queryId = QueryId.New();
            _queries.Add(query.Value, queryId);
        }

        return Task.FromResult(queryId);
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
