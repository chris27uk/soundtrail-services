using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery;

public readonly record struct CatalogSearchCriteria
{
    private CatalogSearchCriteria(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Catalog search criteria is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public static CatalogSearchCriteria From(string value) => new(value);

    public static CatalogSearchCriteria Search(string type, string normalizedQuery) =>
        new($"search:{Require(type, nameof(type))}:{Require(normalizedQuery, nameof(normalizedQuery))}");

    public static CatalogSearchCriteria Artist(ArtistId artistId) => new($"artist:{artistId.Value}");

    public static CatalogSearchCriteria Album(AlbumId albumId) => new($"album:{albumId.Value}");

    public static CatalogSearchCriteria Track(TrackId trackId) => new($"track:{trackId.Value}");

    public override string ToString() => Value;

    public static implicit operator string(CatalogSearchCriteria criteria) => criteria.Value;

    private static string Require(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        return value.Trim();
    }
}
