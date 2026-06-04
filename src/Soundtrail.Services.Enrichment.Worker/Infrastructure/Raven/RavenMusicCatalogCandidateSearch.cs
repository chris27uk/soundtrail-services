using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Soundtrail.Services.Enrichment.Shared.Search;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Documents;
using Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven.Indexes;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.Raven;

public sealed class RavenMusicCatalogCandidateSearch(IDocumentStore documentStore) : IMusicCatalogCandidateSearch
{
    public async Task<IReadOnlyList<MusicCatalogMatch>> SearchAsync(
        NormalizedSearchQuery query,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        var documents = await session
            .Query<RavenTrackDocument, TrackCatalogue_BySearchText>()
            .Search(x => x.SearchText, query.Value)
            .Take(5)
            .ToListAsync(cancellationToken);

        return documents
            .Select(document => new MusicCatalogMatch(
                MusicCatalogId.From(document.Id.Replace("track-catalogue/", string.Empty)),
                Score(document.SearchText, query.Value)))
            .ToArray();
    }

    private static decimal Score(string searchText, string query)
    {
        if (string.Equals(searchText, query, StringComparison.Ordinal))
        {
            return 1.00m;
        }

        if (searchText.Contains(query, StringComparison.Ordinal))
        {
            return 0.90m;
        }

        return 0.80m;
    }
}
