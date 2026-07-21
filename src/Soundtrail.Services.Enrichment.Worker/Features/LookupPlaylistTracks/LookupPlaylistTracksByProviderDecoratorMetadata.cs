using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Aggregates;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupPlaylistTracks;

public sealed class LookupPlaylistTracksByProviderDecoratorMetadata : ILookupDecoratorMetadata<LookupPlaylistTracksByProviderMessage>
{
    public LookupSource Source => LookupSource.Kworb;

    public LookupResultContext CreateContext(LookupPlaylistTracksByProviderMessage message) =>
        new(
            CatalogWorkId.From(new CatalogItemOperation.ChildTracksForPlaylist(message.PlaylistId)),
            message.Id);

    public CatalogItem CreateExistingItem(LookupPlaylistTracksByProviderMessage message, DateTimeOffset observedAt) =>
        new CatalogItem.MusicPlaylist(new Playlist());
}
