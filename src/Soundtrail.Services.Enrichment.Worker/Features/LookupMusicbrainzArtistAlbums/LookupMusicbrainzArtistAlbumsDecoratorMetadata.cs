using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistAlbums;

public sealed class LookupMusicbrainzArtistAlbumsDecoratorMetadata : ILookupDecoratorMetadata<LookupMusicbrainzArtistAlbumsMessage>
{
    public LookupSource Source => LookupSource.MusicBrainz;

    public LookupResultContext CreateContext(LookupMusicbrainzArtistAlbumsMessage message) =>
        new(CatalogWorkId.From(new CatalogItemOperation.ChildAlbumsForArtist(message.ArtistId)), message.Id);

    public CatalogItem CreateExistingItem(LookupMusicbrainzArtistAlbumsMessage message, DateTimeOffset observedAt) =>
        new CatalogItem.MusicArtist(new Artist { Id = message.ArtistId, Name = ArtistName.Empty });
}
