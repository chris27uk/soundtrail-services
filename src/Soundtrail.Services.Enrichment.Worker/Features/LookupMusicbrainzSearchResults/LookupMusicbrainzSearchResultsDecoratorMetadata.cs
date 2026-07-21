using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Enrichment.Worker.Shared.Execution;

namespace Soundtrail.Services.Enrichment.Worker.Features.LookupMusicbrainzSearchResults;

public sealed class LookupMusicbrainzSearchResultsDecoratorMetadata : ILookupDecoratorMetadata<LookupMusicbrainzSearchResultsMessage>
{
    public LookupSource Source => LookupSource.MusicBrainz;

    public LookupResultContext CreateContext(LookupMusicbrainzSearchResultsMessage message) =>
        new(CatalogWorkId.From(message.SearchCriteria), message.Id);

    public CatalogItem CreateExistingItem(LookupMusicbrainzSearchResultsMessage message, DateTimeOffset observedAt) =>
        new CatalogItem.MusicArtist(new Domain.Catalog.Artists.Artist
        {
            Id = Domain.Catalog.Artists.ArtistId.From("musicbrainz-duplicate"),
            Name = Domain.Catalog.Artists.ArtistName.From("Duplicate")
        });
}
