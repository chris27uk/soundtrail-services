using Soundtrail.Services.Features.Search.Contracts;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Api.Infrastructure.Raven;
using Soundtrail.Services.Features.Search.Models;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Tests.Integration.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Features.Search.Contracts;

internal sealed class QueryCacheTestEnvironment : IDisposable
{
    private readonly RavenEmbeddedTestDatabase? raven;

    private QueryCacheTestEnvironment(IQueryCachePort cache)
    {
        Cache = cache;
    }

    private QueryCacheTestEnvironment(
        IQueryCachePort cache,
        RavenEmbeddedTestDatabase raven)
        : this(cache)
    {
        this.raven = raven;
    }

    public IQueryCachePort Cache { get; }

    public static QueryCacheTestEnvironment Create() => CreateRavenEmbedded();

    public void Dispose() => raven?.Dispose();

    private static QueryCacheTestEnvironment CreateRavenEmbedded()
    {
        var raven = RavenEmbeddedTestDatabase.Create();
        return new QueryCacheTestEnvironment(
            new RavenQueryCache(raven.Store),
            raven);
    }
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
