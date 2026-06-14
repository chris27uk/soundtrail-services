using Soundtrail.Domain.Model;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogSearch;

internal sealed class FakeCatalogSearchPort : ICatalogSearchPort
{
    private readonly List<SearchCatalogResult> results = [];

    public void Seed(params SearchCatalogResult[] seededResults)
    {
        results.Clear();
        results.AddRange(seededResults);
    }

    public Task<LocalCatalogSearchResponse> SearchAsync(
        SearchCatalogCommand request,
        CancellationToken cancellationToken)
    {
        var matches = results
            .Where(result => NormalizedSearchQuery.FromText(
                    $"{result.Name} {result.ArtistName} {result.AlbumName}")
                .Value.Contains(request.Query.Value, StringComparison.Ordinal))
            .Where(result => request.Types.Includes(result.Type))
            .Where(result => request.Playback.AllowsAny(result.AvailableProviders))
            .Skip(request.Offset.Value)
            .Take(request.Limit.Value)
            .ToArray();

        return Task.FromResult(new LocalCatalogSearchResponse(matches, null, IsComplete: matches.Length > 0));
    }
}
