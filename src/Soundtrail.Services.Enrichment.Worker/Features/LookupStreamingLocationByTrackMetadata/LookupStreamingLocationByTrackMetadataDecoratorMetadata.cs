using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Catalog.Tracks;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupStreamingLocationByTrackMetadata;

public sealed class LookupStreamingLocationByTrackMetadataDecoratorMetadata : ILookupDecoratorMetadata<LookupStreamingLocationByTrackMetadataMessage>
{
    public LookupSource Source => LookupSource.Odesli;

    public LookupResultContext CreateContext(LookupStreamingLocationByTrackMetadataMessage message) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.StreamingLocationForTrack(message.TrackId)),
            message.Id);

    public CatalogItem CreateExistingItem(LookupStreamingLocationByTrackMetadataMessage message, DateTimeOffset observedAt) =>
        new CatalogItem.MusicTrack(new Track(message.TrackId));
}
