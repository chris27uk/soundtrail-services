using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzArtistTracks;

public sealed class LookupMusicbrainzArtistTracksDecoratorMetadata : ILookupDecoratorMetadata<LookupMusicbrainzArtistTracksMessage>
{
    public LookupSource Source => LookupSource.MusicBrainz;

    public LookupResultContext CreateContext(LookupMusicbrainzArtistTracksMessage message) =>
        new(CatalogWorkId.From(new CatalogItemOperation.ChildTracksForArtist(message.ArtistId)), message.Id);

    public CatalogItem CreateExistingItem(LookupMusicbrainzArtistTracksMessage message, DateTimeOffset observedAt) =>
        new CatalogItem.MusicArtist(new Artist { Id = message.ArtistId, Name = ArtistName.Empty });
}
