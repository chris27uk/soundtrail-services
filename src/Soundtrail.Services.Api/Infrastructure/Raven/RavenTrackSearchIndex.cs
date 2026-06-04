using Raven.Client.Documents;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Features.Search.TrackSearch;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenTrackSearchIndex(IDocumentStore documentStore) : ITrackSearchPort
{
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        NormalizedSearchQuery query,
        Limit limit,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        var documents = await session
            .Query<RavenTrackDocument, Indexes.TrackCatalogue_BySearchText>()
            .Search(x => x.SearchText, query.Value)
            .Take(limit.Value)
            .ToListAsync(cancellationToken);

        return documents
            .Select(document => new SearchResult(
                TrackTitle.From(document.Title),
                ArtistName.From(document.Artist),
                string.IsNullOrWhiteSpace(document.Isrc) ? null : Isrc.From(document.Isrc),
                string.IsNullOrWhiteSpace(document.Mbid) ? null : Mbid.From(document.Mbid),
                string.IsNullOrWhiteSpace(document.AppleId) ? null : AppleId.From(document.AppleId),
                string.IsNullOrWhiteSpace(document.SpotifyId) ? null : SpotifyId.From(document.SpotifyId),
                ConfidenceScore.From(0.95)))
            .ToArray();
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
