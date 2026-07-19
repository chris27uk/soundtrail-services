using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Abstractions.EventSourcing;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Catalog.Events;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Events;

namespace Soundtrail.Services.Internal.Projector.Features.OnLookupRecorded;

public sealed class StreamingLocationDiscoveredProjectorHandler(
    IEventStreamRepository<ArtistId> repository)
{
    public async Task Handle(StreamingLocationDiscovered @event, CancellationToken cancellationToken = default)
    {
        var artistId = @event.Hierarchy.ArtistId
            ?? throw new InvalidOperationException("StreamingLocationDiscovered must include artist ownership hierarchy.");
        var trackId = @event.MusicCatalogId?.Match(
            track => track.Id,
            _ => throw new InvalidOperationException("StreamingLocationDiscovered must refer to a track."),
            _ => throw new InvalidOperationException("StreamingLocationDiscovered must refer to a track."),
            _ => throw new InvalidOperationException("StreamingLocationDiscovered must refer to a track."))
            ?? throw new InvalidOperationException("StreamingLocationDiscovered must include a track id.");
        var (stream, catalog) = await ArtistCatalog.LoadAsync(repository, artistId, cancellationToken);
        catalog.StreamingLocationDiscovered(
            trackId,
            new Domain.Catalog.StreamingLocation(
                @event.Provider,
                @event.ExternalId,
                @event.Url,
                @event.SourceProvider,
                @event.ObservedAt));
        await catalog.SaveAsync(
            repository,
            stream,
            CommandId.For($"StreamingLocationDiscovered:{artistId.Value}:{trackId.Value}:{@event.ObservedAt:O}"),
            cancellationToken);
    }
}
