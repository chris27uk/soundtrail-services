using System.Collections.Concurrent;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Api.Infrastructure.TableStorage;

public sealed class AzureTableTrackLookup : ICatalogLookupPort
{
    private readonly ConcurrentDictionary<string, Track> tracksByIdentifier = new();

    public Task<Track?> LookupAsync(
        CatalogLookupRequest request,
        CancellationToken cancellationToken)
    {
        var track =
            Find(request.Isrc, "isrc:") ??
            Find(request.AppleId, "apple:") ??
            Find(request.Mbid, "mbid:") ??
            Find(request.SpotifyId, "spotify:");

        if (track is not null &&
            request.DurationMs is not null &&
            track.Duration?.Value != request.DurationMs.Value.Value)
        {
            track = null;
        }

        return Task.FromResult(track);
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);

    public void Seed(params Track[] tracks)
    {
        tracksByIdentifier.Clear();

        foreach (var track in tracks)
        {
            Store(track.Isrc?.Value, "isrc:", track);
            Store(track.AppleId?.Value, "apple:", track);
            Store(track.Mbid?.Value, "mbid:", track);
            Store(track.SpotifyId?.Value, "spotify:", track);
        }
    }

    private Track? Find(string? value, string prefix) =>
        value is null
            ? null
            : tracksByIdentifier.TryGetValue($"{prefix}{value}", out var track)
                ? track
                : null;

    private void Store(string? value, string prefix, Track track)
    {
        if (value is not null)
        {
            tracksByIdentifier[$"{prefix}{value}"] = track;
        }
    }
}
