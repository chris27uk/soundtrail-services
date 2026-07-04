using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup;

public sealed class CatalogSearchPlannedForLookupHandler(
    ILoadCatalogSearchPlannedMusicTrackPort loadMusicTrackPort,
    ICommandBus commandBus) : IHandler<CatalogSearchPlannedForLookupCommand>
{
    public async Task Handle(
        CatalogSearchPlannedForLookupCommand request,
        CancellationToken cancellationToken = default)
    {
        foreach (var planned in request.Events
                     .OrderBy(x => x.Version)
                     .Where(x => x.Event is DiscoveryPlanned))
        {
            var commandToSend = await BuildLookupCommandAsync(request.Events, planned, cancellationToken);
            if (commandToSend is null)
            {
                continue;
            }

            await commandBus.SendAsync(commandToSend, cancellationToken);
        }
    }

    private async Task<ICommand?> BuildLookupCommandAsync(
        IReadOnlyList<VersionedCatalogSearchDiscoveryEvent> events,
        VersionedCatalogSearchDiscoveryEvent plannedEvent,
        CancellationToken cancellationToken)
    {
        var planned = (DiscoveryPlanned)plannedEvent.Event;
        var candidate = events
            .Where(x => x.Version <= plannedEvent.Version)
            .Select(x => x.Event)
            .OfType<CatalogCandidateIdentified>()
            .LastOrDefault();

        if (candidate is null)
        {
            return null;
        }

        var track = await loadMusicTrackPort.LoadAsync(candidate.MusicCatalogId, cancellationToken);
        if (track?.IsPlayable == true)
        {
            return null;
        }

        var musicCatalogId = candidate.MusicCatalogId;
        var hierarchy = track is null || string.IsNullOrWhiteSpace(track.ArtistId) && string.IsNullOrWhiteSpace(track.AlbumId)
            ? null
            : new CatalogTrackHierarchy(
                string.IsNullOrWhiteSpace(track.ArtistId) ? null : ArtistId.From(track.ArtistId),
                string.IsNullOrWhiteSpace(track.AlbumId) ? null : AlbumId.From(track.AlbumId));

        if (!string.IsNullOrWhiteSpace(track?.ResolvedIsrc ?? track?.Isrc))
        {
            return new LookupStreamingLocationsCommand(
                LookupStreamingLocationsCommand.Id(musicCatalogId),
                musicCatalogId,
                planned.Priority,
                planned.PlannedAt,
                CorrelationId.New(),
                LookupCriteria.ExactIsrc(track!.ResolvedIsrc ?? track.Isrc!),
                hierarchy);
        }

        var title = track?.ResolvedTitle ?? track?.Title;
        var artist = track?.ResolvedArtist ?? track?.Artist;
        if (string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(artist))
        {
            return new LookupTrackMetadataCommand(
                CommandId.For($"LookupTrackMetadata:{musicCatalogId.Value}"),
                musicCatalogId,
                planned.Priority,
                planned.PlannedAt,
                CorrelationId.New(),
                planned.SearchCriteria,
                hierarchy);
        }

        return new LookupTrackMetadataCommand(
            CommandId.For($"LookupTrackMetadata:{musicCatalogId.Value}"),
            musicCatalogId,
            planned.Priority,
            planned.PlannedAt,
            CorrelationId.New(),
            LookupCriteria.ByTrackArtistAlbum(title, artist, track.AlbumTitle),
            hierarchy);
    }
}
