using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;

namespace Soundtrail.Domain.Discovery;

public static class MusicSearchTermSet
{
    public static IReadOnlyList<MusicSearchCriteria> ForResolvedTrack(
        MusicCatalogId musicCatalogId,
        ArtistId? artistId,
        AlbumId? albumId,
        MusicSearchCriteria? originatingSearchTerm = null)
    {
        _ = musicCatalogId;
        _ = artistId;
        _ = albumId;
        var values = new HashSet<MusicSearchCriteria>();
        var searchTerms = new List<MusicSearchCriteria>();

        Add(originatingSearchTerm);

        return searchTerms;

        void Add(MusicSearchCriteria? item)
        {
            if (item is null)
            {
                return;
            }

            if (values.Add(item))
            {
                searchTerms.Add(item);
            }
        }
    }
}
