using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Model;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnMusicTrackSearchStarted.Support;
using Soundtrail.Translators.Discovery;

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
            var appliedEventId = $"{MusicSearchTermPersistentIdTranslator.ToPersistentId(command.SearchCriteria)}:{item.Version}";

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
                    @event.SearchCriteria,
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

        foreach (var searchTerm in BuildSearchTerms(@event, track))
        {
            var existing = await loadTrackingPort.LoadAsync(searchTerm, cancellationToken);
            if (existing is not null
                && string.Equals(existing.MusicCatalogId, @event.MusicCatalogId.Value, StringComparison.Ordinal)
                && existing.UpdatedAt >= @event.StartedAt)
            {
                continue;
            }

            await saveTrackingPort.SaveAsync(
                searchTerm,
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

    private static IReadOnlyList<MusicSearchCriteria> BuildSearchTerms(
        MusicTrackSearchStarted @event,
        CatalogSearchStartedMusicTrack? track)
    {
        return MusicSearchTermSet.ForResolvedTrack(
            @event.MusicCatalogId,
            string.IsNullOrWhiteSpace(track?.ArtistId) ? null : ArtistId.From(track.ArtistId),
            string.IsNullOrWhiteSpace(track?.AlbumId) ? null : AlbumId.From(track.AlbumId),
            @event.SearchCriteria);
    }
}
