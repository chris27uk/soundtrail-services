using Raven.Client.Documents;
using Soundtrail.Contracts.Persistence;
using Soundtrail.Domain.Catalog.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnMusicDataRequested;

public sealed class CatalogSearchCandidateProjectorHandler(IDocumentStore documentStore)
{
    public Task Handle(ArtistDiscovered @event, CancellationToken cancellationToken = default) =>
        UpsertCandidateAsync(
            @event.Artist.Id.Value,
            "Artist",
            @event.Artist.Name.Value,
            @event.ObservedAt,
            cancellationToken);

    public Task Handle(AlbumDiscovered @event, CancellationToken cancellationToken = default) =>
        UpsertCandidateAsync(
            @event.Album.AlbumId.StableValue,
            "Album",
            @event.Album.AlbumTitle ?? string.Empty,
            @event.ObservedAt,
            cancellationToken);

    public Task Handle(TrackDiscovered @event, CancellationToken cancellationToken = default) =>
        UpsertCandidateAsync(
            @event.Track.TrackId.Value,
            "Track",
            $"{@event.Track.Title} {@event.Track.ArtistName}".Trim(),
            @event.ObservedAt,
            cancellationToken);

    private async Task UpsertCandidateAsync(
        string catalogItemId,
        string candidateKind,
        string searchText,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        using var session = documentStore.OpenAsyncSession();
        await session.StoreAsync(
            new CatalogSearchCandidateRecordDto
            {
                Id = CatalogSearchCandidateRecordDto.GetDocumentId(catalogItemId),
                CatalogItemId = catalogItemId,
                CandidateKind = candidateKind,
                SearchText = searchText,
                UpdatedAt = updatedAt
            },
            cancellationToken);

        await session.SaveChangesAsync(cancellationToken);
    }
}
