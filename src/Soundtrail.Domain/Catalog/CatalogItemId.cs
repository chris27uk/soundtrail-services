using Dunet;
using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog;

[Union]
public partial record CatalogItemId : IValueType
{
    public partial record Track(TrackId Value);

    public partial record Artist(ArtistId Value);

    public partial record Album(CatalogAlbumId Value);

    public CatalogEntityKind EntityKind =>
        this switch
        {
            Track => CatalogEntityKind.Track,
            Artist => CatalogEntityKind.Artist,
            Album => CatalogEntityKind.Album,
            _ => throw new InvalidOperationException($"Unsupported catalog item id type '{GetType().Name}'.")
        };

    public string StableValue =>
        this switch
        {
            Track(var trackId) => trackId.Value,
            Artist(var artistId) => artistId.Value,
            Album(var albumId) => albumId.StableValue,
            _ => throw new InvalidOperationException($"Unsupported catalog item id type '{GetType().Name}'.")
        };

    public TrackId RequireTrackId() =>
        this switch
        {
            Track(var trackId) => trackId,
            _ => throw new InvalidOperationException("Catalog item id must be a track id.")
        };
}
