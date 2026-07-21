using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzAlbumTracks;

public sealed class LookupMusicbrainzAlbumTracksDecoratorMetadata : ILookupDecoratorMetadata<LookupMusicbrainzAlbumTracksMessage>
{
    public LookupSource Source => LookupSource.MusicBrainz;

    public LookupResultContext CreateContext(LookupMusicbrainzAlbumTracksMessage message) =>
        new(CatalogWorkId.From(new CatalogItemOperation.ChildTracksForAlbum(message.AlbumId)), message.Id);

    public CatalogItem CreateExistingItem(LookupMusicbrainzAlbumTracksMessage message, DateTimeOffset observedAt) =>
        new CatalogItem.MusicAlbum(new Album(message.AlbumId, null, null, null, null, observedAt));
}
