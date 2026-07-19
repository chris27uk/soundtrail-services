using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using CatalogItemOperation = Soundtrail.Domain.Discovery.CatalogItemOperation;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupCompleted.Collaborators;

internal static class LookupCompletedEventRecorder
{
    public static void RecordSuccess(
        DiscoveryHistory history,
        LookupResult.Succeeded result,
        LookupCompletionContext context)
    {
        result.Value.Match(
            entries =>
            {
                foreach (var entry in entries.Values)
                {
                    history.Discover(entry, result.CompletedAt);
                }

                if (context.Target is EnrichmentTarget.KnownCatalogItemOperation(CatalogItemOperation.ChildTracksForPlaylist(var playlistId)))
                {
                    history.DiscoverPlaylistTracks(
                        playlistId,
                        entries.Values
                            .Select(entry => entry.Item)
                            .OfType<Soundtrail.Domain.Catalog.CatalogItem.MusicTrack>()
                            .Select(track => track.Track.TrackId)
                            .ToArray(),
                        result.CompletedAt);
                }
                return 0;
            },
            link =>
            {
                history.DiscoverStreamingLocation(link.ArtistId, link.TrackId, link.Value, result.CompletedAt);
                return 0;
            });
    }
}
