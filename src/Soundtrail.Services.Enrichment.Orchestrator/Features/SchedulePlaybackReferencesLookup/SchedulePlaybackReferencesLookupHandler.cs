using Soundtrail.Contracts.Common;
using Soundtrail.Domain;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Commands;
using Soundtrail.Domain.Model;

namespace Soundtrail.Services.Enrichment.Orchestrator.Features.SchedulePlaybackReferencesLookup;

public sealed class SchedulePlaybackReferencesLookupHandler(ICommandBus commandBus)
{
    public Task Handle(
        SchedulePlaybackReferencesLookupCommand command,
        CancellationToken cancellationToken = default)
    {
        return commandBus.SendAsync(
            new ResolvePlaybackReferencesCommand(
                CommandId.For($"ResolvePlaybackReferences:{command.MusicCatalogId.Value}"),
                command.MusicCatalogId,
                command.Priority,
                command.ObservedAt,
                command.CorrelationId,
                ToSearchTerm(command),
                command.ArtistId is null && command.AlbumId is null
                    ? null
                    : new CatalogTrackHierarchy(
                        command.ArtistId is null ? null : ArtistId.From(command.ArtistId),
                        command.AlbumId is null ? null : AlbumId.From(command.AlbumId))),
            cancellationToken);
    }

    private static MusicSearchTerm ToSearchTerm(SchedulePlaybackReferencesLookupCommand command) =>
        !string.IsNullOrWhiteSpace(command.SearchTerm.Isrc)
            ? MusicSearchTerm.ByIsrc(command.SearchTerm.Isrc)
            : MusicSearchTerm.ByTrackArtistAlbum(
                command.SearchTerm.Title ?? throw new InvalidOperationException("Playback reference lookup requires a title when no ISRC is present."),
                command.SearchTerm.Artist ?? throw new InvalidOperationException("Playback reference lookup requires an artist when no ISRC is present."),
                command.SearchTerm.Album);
}
