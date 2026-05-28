using Soundtrail.Services.Application.Ports;
using Soundtrail.Services.Application.Search;
using Soundtrail.Services.Domain.Tracks;
using Soundtrail.Services.Domain.ValueTypes;
using Soundtrail.Services.Api.Infrastructure.TableStorage;

namespace Soundtrail.Services.Tests.Infrastructure.Contracts;

internal sealed class QueryCacheTestEnvironment
{
    private QueryCacheTestEnvironment(IQueryCachePort cache)
    {
        Cache = cache;
    }

    public IQueryCachePort Cache { get; }

    public static QueryCacheTestEnvironment Create(StorageMode mode) =>
        mode switch
        {
            StorageMode.Fake => new(new FakeQueryCachePort()),
            StorageMode.AzureTable => new(new AzureTableQueryCache()),
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };
}

internal sealed class FakeQueryCachePort : IQueryCachePort
{
    private readonly Dictionary<string, SearchMusicResponse> _responses = new();

    public Task<SearchMusicResponse?> GetAsync(NormalizedSearchQuery query, CancellationToken cancellationToken)
    {
        _responses.TryGetValue(query.Value, out var response);
        return Task.FromResult(response);
    }

    public Task StoreAsync(
        NormalizedSearchQuery query,
        SearchMusicResponse response,
        TimeSpan timeToLive,
        CancellationToken cancellationToken)
    {
        _responses[query.Value] = response;
        return Task.CompletedTask;
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}

internal static class ContractKnownTracks
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
