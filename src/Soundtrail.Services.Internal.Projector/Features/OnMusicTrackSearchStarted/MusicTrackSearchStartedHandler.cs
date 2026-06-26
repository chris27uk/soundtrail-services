using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted;

public sealed class MusicTrackSearchStartedHandler(
    ILoadPotentialCatalogLookupWorkPort loadWorkPort,
    ISavePotentialCatalogLookupWorkPort saveWorkPort,
    ILoadCatalogSearchStartedMusicTrackPort loadMusicTrackPort,
    ILoadCatalogSearchStartedTrackingPort loadTrackingPort,
    ISaveCatalogSearchStartedTrackingPort saveTrackingPort,
    ICommandBus commandBus)
{
    public async Task Handle(
        MusicTrackSearchStartedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var @event = (MusicTrackSearchStarted)item.Event;
            var workDocument = await loadWorkPort.LoadAsync(@event.MusicCatalogId, cancellationToken);
            var appliedEventId = $"{command.Criteria.Value}:{item.Version}";

            if (workDocument.AppliedSearchStartEventIds.Contains(appliedEventId, StringComparer.Ordinal))
            {
                continue;
            }

            Apply(workDocument, @event, appliedEventId);
            await UpsertTrackingsAsync(@event, cancellationToken);
            await saveWorkPort.SaveAsync(workDocument, cancellationToken);
            await commandBus.SendAsync(
                new AssessMusicTrackCommand(
                    AssessMusicTrackCommand.Id(@event.MusicCatalogId, @event.StartedAt),
                    @event.CorrelationId,
                    @event.StartedAt,
                    LookupPriorityBand.Low,
                    @event.MusicCatalogId,
                    @event.Criteria,
                    @event.TrustLevel,
                    @event.RiskScore),
                cancellationToken);
        }

    }

    private async Task UpsertTrackingsAsync(
        MusicTrackSearchStarted @event,
        CancellationToken cancellationToken)
    {
        var track = await loadMusicTrackPort.LoadAsync(@event.MusicCatalogId, cancellationToken);

        foreach (var criteria in BuildCriteria(@event, track))
        {
            var existing = await loadTrackingPort.LoadAsync(criteria, cancellationToken);
            if (existing is not null
                && string.Equals(existing.MusicCatalogId, @event.MusicCatalogId.Value, StringComparison.Ordinal)
                && existing.UpdatedAt >= @event.StartedAt)
            {
                continue;
            }

            await saveTrackingPort.SaveAsync(
                criteria,
                @event.MusicCatalogId,
                @event.StartedAt,
                cancellationToken);
        }
    }

    private void Apply(
        PotentialCatalogLookupWorkState document,
        MusicTrackSearchStarted @event,
        string appliedEventId)
    {
        document.RequestCount += 1;
        document.HighestTrustLevelSeen = Math.Max(document.HighestTrustLevelSeen, @event.TrustLevel);
        document.RiskScore = Math.Max(document.RiskScore, @event.RiskScore);
        if (string.Equals(document.Status, "Pending", StringComparison.OrdinalIgnoreCase)
            && @event.RiskScore >= 90)
        {
            document.Status = "Ignored";
        }

        document.AppliedSearchStartEventIds.Add(appliedEventId);
    }

    private static IReadOnlyList<CatalogSearchCriteria> BuildCriteria(
        MusicTrackSearchStarted @event,
        CatalogSearchStartedMusicTrack? track)
    {
        var criteria = new List<CatalogSearchCriteria> { @event.Criteria };
        var seen = new HashSet<string>(StringComparer.Ordinal) { @event.Criteria.Value };

        Add(CatalogSearchCriteria.Track(TrackId.From(@event.MusicCatalogId.Value)));

        if (!string.IsNullOrWhiteSpace(track?.ArtistId))
        {
            Add(CatalogSearchCriteria.Artist(ArtistId.From(track.ArtistId)));
        }

        if (!string.IsNullOrWhiteSpace(track?.AlbumId))
        {
            Add(CatalogSearchCriteria.Album(AlbumId.From(track.AlbumId)));
        }

        return criteria;

        void Add(CatalogSearchCriteria value)
        {
            if (seen.Add(value.Value))
            {
                criteria.Add(value);
            }
        }
    }
}
