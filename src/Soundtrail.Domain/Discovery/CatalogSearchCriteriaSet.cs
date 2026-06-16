using Soundtrail.Domain.Catalog;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Discovery;

public static class CatalogSearchCriteriaSet
{
    public static IReadOnlyList<CatalogSearchCriteria> ForResolvedTrack(
        MusicCatalogId musicCatalogId,
        ArtistId? artistId,
        AlbumId? albumId,
        CatalogSearchCriteria? originatingCriteria = null)
    {
        var values = new HashSet<string>(StringComparer.Ordinal);
        var criteria = new List<CatalogSearchCriteria>();

        Add(originatingCriteria);
        Add(CatalogSearchCriteria.Track(TrackId.From(musicCatalogId.Value)));

        if (artistId is not null)
        {
            Add(CatalogSearchCriteria.Artist(artistId.Value));
        }

        if (albumId is not null)
        {
            Add(CatalogSearchCriteria.Album(albumId.Value));
        }

        return criteria;

        void Add(CatalogSearchCriteria? item)
        {
            if (item is null)
            {
                return;
            }

            if (values.Add(item.Value.Value))
            {
                criteria.Add(item.Value);
            }
        }
    }
}
