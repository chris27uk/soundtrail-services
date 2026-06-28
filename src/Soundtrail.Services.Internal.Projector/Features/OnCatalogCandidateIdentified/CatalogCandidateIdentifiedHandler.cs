using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified.Support;
using Soundtrail.Adapters.Discovery;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogCandidateIdentified;

public sealed class CatalogCandidateIdentifiedHandler(
    ILoadPotentialCatalogLookupWorkPort loadWorkPort,
    ISavePotentialCatalogLookupWorkPort saveWorkPort,
    ILoadCatalogCandidateMusicTrackPort loadMusicTrackPort,
    ILoadCatalogCandidateTrackingPort loadTrackingPort,
    ISaveCatalogCandidateTrackingPort saveTrackingPort,
    ICommandBus commandBus)
{
    public async Task Handle(
        CatalogCandidateIdentifiedCommand command,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in command.Events.OrderBy(x => x.Version))
        {
            var @event = (CatalogCandidateIdentified)item.Event;
            var workDocument = await loadWorkPort.LoadAsync(@event.MusicCatalogId, cancellationToken);
            var appliedEventId = $"{DiscoveryQueryKey.StableValueFor(command.SearchCriteria)}:{item.Version}";

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
        CatalogCandidateIdentified @event,
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
        CatalogCandidateIdentified @event,
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
        CatalogCandidateIdentified @event,
        CatalogCandidateMusicTrack? track)
    {
        return MusicSearchTermSet.ForResolvedTrack(
            @event.MusicCatalogId,
            string.IsNullOrWhiteSpace(track?.ArtistId) ? null : ArtistId.From(track.ArtistId),
            string.IsNullOrWhiteSpace(track?.AlbumId) ? null : AlbumId.From(track.AlbumId),
            @event.SearchCriteria);
    }
}
