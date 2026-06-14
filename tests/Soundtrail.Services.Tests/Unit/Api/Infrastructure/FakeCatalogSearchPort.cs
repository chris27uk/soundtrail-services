using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure;

internal sealed class FakeCatalogSearchPort : ICatalogSearchPort
{
    private readonly IReadOnlyList<SearchCatalogResult> seededResults;
    private readonly SearchDiscovery? discovery;
    private readonly bool isComplete;

    public FakeCatalogSearchPort(
        IReadOnlyList<SearchCatalogResult>? seededResults = null,
        SearchDiscovery? discovery = null,
        bool isComplete = true)
    {
        this.seededResults = seededResults ?? [];
        this.discovery = discovery;
        this.isComplete = isComplete;
    }

    public Task<LocalCatalogSearchResponse> SearchAsync(
        SearchCatalogCommand request,
        CancellationToken cancellationToken)
    {
        var queryText = request.Query.Value;

        var matches = seededResults
            .Where(result => NormalizedSearchQuery.FromText(
                    $"{result.Name} {result.ArtistName} {result.AlbumName}")
                .Value.Contains(queryText, StringComparison.Ordinal))
            .Where(result => request.Types.Includes(result.Type))
            .Where(result => request.Playback.AllowsAny(result.AvailableProviders))
            .Skip(request.Offset.Value)
            .Take(request.Limit.Value)
            .ToArray();

        return Task.FromResult(new LocalCatalogSearchResponse(matches, discovery, isComplete));
    }
}
