using System.Collections.Concurrent;
using Soundtrail.Services.Application.Ports;
using Soundtrail.Services.Domain.Tracks;
using Soundtrail.Services.Domain.ValueTypes;

namespace Soundtrail.Services.Api.Infrastructure.Search;

public sealed class SqliteTrackSearchIndex : ITrackSearchPort
{
    private readonly ConcurrentBag<SearchResult> _tracks = new();

    public SqliteTrackSearchIndex()
    {
    }

    public SqliteTrackSearchIndex(IEnumerable<SearchResult> seedTracks)
    {
        foreach (var track in seedTracks)
        {
            _tracks.Add(track);
        }
    }

    public Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken)
    {
        var results = _tracks
            .Where(track => Matches(query, track))
            .OrderByDescending(track => track.Confidence.Value)
            .Take(limit.Value)
            .ToArray();

        return Task.FromResult<IReadOnlyList<SearchResult>>(results);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    public void Seed(params SearchResult[] tracks)
    {
        foreach (var track in tracks)
        {
            _tracks.Add(track);
        }
    }

    private static bool Matches(NormalizedSearchQuery query, SearchResult result)
    {
        var haystack = NormalizedSearchQuery.FromText($"{result.Title.Value} {result.Artist.Value}");
        return haystack.Value.Contains(query.Value, StringComparison.Ordinal);
    }
}
