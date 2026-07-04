using Dunet;

namespace Soundtrail.Domain.Catalog;

[Union]
public partial record CatalogItemId
{
    public partial record Track(TrackId Value);

    public partial record Artist(ArtistId Value);

    public partial record Album(AlbumId Value);

    public string NormalisedIdentifier =>
        this switch
        {
            Track(var trackId) => trackId.Value,
            Artist(var artistId) => artistId.Value,
            Album(var albumId) => albumId.StableValue,
            _ => throw new InvalidOperationException($"Unsupported catalog item id type '{GetType().Name}'.")
        };
}
