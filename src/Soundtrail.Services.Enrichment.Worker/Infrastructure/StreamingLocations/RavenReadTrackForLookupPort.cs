using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Services.Enrichment.Worker.Shared.StreamingLocations;

namespace Soundtrail.Services.Enrichment.Worker.Infrastructure.StreamingLocations;

public sealed class RavenReadTrackForLookupPort(IDocumentStore documentStore) : IReadTrackForLookupPort
{
    public async Task<TrackLookupContext?> ReadAsync(TrackId trackId, CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();

        var trackDocument = await session.LoadAsync<CatalogTrackRecordDto>(
            CatalogTrackRecordDto.GetDocumentId(trackId.Value),
            cancellationToken);

        if (trackDocument is null)
        {
            return null;
        }

        var artistTracks = await session.Query<CatalogArtistTracksRecordDto>()
            .Customize(query => query.WaitForNonStaleResults())
            .SingleOrDefaultAsync(
                x => x.Tracks.Any(track => track.TrackId == trackId.Value),
                cancellationToken);

        if (artistTracks is null)
        {
            return null;
        }

        return new TrackLookupContext(
            ArtistId.From(artistTracks.ArtistId),
            trackId,
            trackDocument.Title,
            trackDocument.ArtistName,
            trackDocument.Isrc);
    }
}
