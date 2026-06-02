using Raven.Client.Documents;
using Raven.Client.Documents.Linq;
using Soundtrail.Services.Api.Infrastructure.Raven.Documents;
using Soundtrail.Services.Features.CatalogLookup.Contracts;
using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;

namespace Soundtrail.Services.Api.Infrastructure.Raven;

public sealed class RavenTrackLookup(IDocumentStore documentStore) : ICatalogLookupPort
{
    public async Task<Track?> LookupAsync(
        CatalogLookupRequest request,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        IQueryable<RavenTrackDocument> query = session.Query<RavenTrackDocument>();
        if (!string.IsNullOrWhiteSpace(request.Isrc))
        {
            query = query.Where(x => x.Isrc == request.Isrc);
        }
        else if (!string.IsNullOrWhiteSpace(request.AppleId))
        {
            query = query.Where(x => x.AppleId == request.AppleId);
        }
        else if (!string.IsNullOrWhiteSpace(request.Mbid))
        {
            query = query.Where(x => x.Mbid == request.Mbid);
        }
        else if (!string.IsNullOrWhiteSpace(request.SpotifyId))
        {
            query = query.Where(x => x.SpotifyId == request.SpotifyId);
        }

        var document = await query.FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        if (request.DurationMs is not null &&
            document.DurationMs is not null &&
            document.DurationMs != request.DurationMs.Value.Value)
        {
            return null;
        }

        return document.ToDomainTrack();
    }

    public Task<bool> IsReadyAsync(CancellationToken cancellationToken) => Task.FromResult(true);
}
