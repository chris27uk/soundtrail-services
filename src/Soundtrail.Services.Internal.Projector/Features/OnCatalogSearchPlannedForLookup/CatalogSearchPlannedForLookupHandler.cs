using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Discovery.Events;
using Soundtrail.Domain.Enrichment.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Ports;
using Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup.Support;

namespace Soundtrail.Services.Internal.Projector.Features.OnCatalogSearchPlannedForLookup;

public sealed class CatalogSearchPlannedForLookupHandler(
    ILoadCatalogSearchPlannedTrackingPort loadTrackingPort,
    ILoadCatalogSearchPlannedMusicTrackPort loadMusicTrackPort,
    ICommandBus commandBus) : IHandler<CatalogSearchPlannedForLookupCommand>
{
    public async Task Handle(
        CatalogSearchPlannedForLookupCommand request,
        CancellationToken cancellationToken = default)
    {
        foreach (var planned in request.Events
                     .OrderBy(x => x.Version)
                     .Select(x => x.Event)
                     .OfType<DiscoveryPlanned>())
        {
            var commandToSend = await BuildLookupCommandAsync(planned, cancellationToken);
            if (commandToSend is null)
            {
                continue;
            }

            await commandBus.SendAsync(commandToSend, cancellationToken);
        }
    }

    private async Task<ICommand?> BuildLookupCommandAsync(
        DiscoveryPlanned planned,
        CancellationToken cancellationToken)
    {
        var tracking = await loadTrackingPort.LoadAsync(planned.SearchCriteria, cancellationToken);
        if (tracking is null)
        {
            return null;
        }

        var track = await loadMusicTrackPort.LoadAsync(
            MusicCatalogId.From(tracking.MusicCatalogId),
            cancellationToken);
        if (track is null || track.IsPlayable)
        {
            return null;
        }

        var musicCatalogId = MusicCatalogId.From(tracking.MusicCatalogId);
        var hierarchy = string.IsNullOrWhiteSpace(track.ArtistId) && string.IsNullOrWhiteSpace(track.AlbumId)
            ? null
            : new CatalogTrackHierarchy(
                string.IsNullOrWhiteSpace(track.ArtistId) ? null : ArtistId.From(track.ArtistId),
                string.IsNullOrWhiteSpace(track.AlbumId) ? null : AlbumId.From(track.AlbumId));

        if (!string.IsNullOrWhiteSpace(track.ResolvedIsrc ?? track.Isrc))
        {
            return new LookupStreamingLocationsCommand(
                LookupStreamingLocationsCommand.Id(musicCatalogId),
                musicCatalogId,
                planned.Priority,
                planned.PlannedAt,
                CorrelationId.New(),
                MusicSearchCriteria.ByIsrc(track.ResolvedIsrc ?? track.Isrc!),
                hierarchy);
        }

        var title = track.ResolvedTitle ?? track.Title;
        var artist = track.ResolvedArtist ?? track.Artist;
        if (string.IsNullOrWhiteSpace(title)
            || string.IsNullOrWhiteSpace(artist))
        {
            return null;
        }

        return new LookupTrackMetadataCommand(
            CommandId.For($"LookupTrackMetadata:{musicCatalogId.Value}"),
            musicCatalogId,
            planned.Priority,
            planned.PlannedAt,
            CorrelationId.New(),
            MusicSearchCriteria.ByTrackArtistAlbum(title, artist, track.AlbumTitle),
            hierarchy);
    }
}
