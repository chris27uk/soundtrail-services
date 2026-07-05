using Raven.Client.Documents.Session;
using Soundtrail.Contracts.EventSourcing;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Projection;
using Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.EventStore;
using Soundtrail.Adapters.Registry;

namespace Soundtrail.Tools.MusicBrainzImport.Features.ReplayCatalogProjection.Adapters;

public sealed class RavenLoadMusicTrackEventsForCatalogReplay(
    IAsyncDocumentSession session,
    ITypeRegistry translator) : ILoadMusicTrackEventsForCatalogReplayPort
{
    public async Task<IReadOnlyList<VersionedCatalogEvent>> LoadAsync(
        ArtistId artistId,
        CancellationToken cancellationToken)
    {
        var events = (await session.Advanced.LoadStartingWithAsync<RavenStoredEventRecord>(
                $"artist-catalog-events/{artistId.Value}/"))
            .OrderBy(x => x.Version)
            .Select(x => new VersionedCatalogEvent(
                x.Version,
                translator.ToDomainObject<IDomainEvent>(
                    x.Body ?? throw new InvalidOperationException($"Stored event '{x.Id}' is missing a body."))))
            .ToArray();

        return events;
    }
}
