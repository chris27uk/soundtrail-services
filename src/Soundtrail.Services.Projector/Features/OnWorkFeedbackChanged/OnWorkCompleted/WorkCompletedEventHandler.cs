using Soundtrail.Adapters.Projection;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Services.Internal.Projector.Features.OnPlaylistTracksDiscovered.Adapters;
using Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.Adapters;

namespace Soundtrail.Services.Internal.Projector.Features.OnWorkFeedbackChanged.OnWorkCompleted
{
    public class WorkCompletedEventHandler(
        IStoreDiscoveryFeedbackPort storeDiscoveryFeedbackPort,
        IStorePlaylistTracksReadModelPort storePlaylistTracksReadModelPort) : IProjectionEventHandler<WorkCompleted>
    {
        private const string LookupCompletedReason = "Lookup completed.";

        public async Task HandleAsync(WorkCompleted @event, CancellationToken cancellationToken = default)
        {
            await storeDiscoveryFeedbackPort.StoreAsync(@event, cancellationToken);

            // ArtistCatalogChanged may Repair before the playlist row exists. A second Repair when
            // streaming WorkCompleted lands closes that race (playlist discovery stuck on
            // "Track streaming projection is still catching up.").
            if (@event.Target is EnrichmentTarget.KnownCatalogItemOperation(
                    CatalogItemOperation.StreamingLocationForTrack(var trackId))
                && string.Equals(@event.Reason, LookupCompletedReason, StringComparison.Ordinal))
            {
                await storePlaylistTracksReadModelPort.RepairTrackAsync(trackId, cancellationToken);
            }
        }
    }
}
