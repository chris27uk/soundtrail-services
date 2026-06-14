using Soundtrail.Domain.Catalog;

namespace Soundtrail.Domain.Discovery;

public readonly record struct DiscoveryQueryKey
{
    private DiscoveryQueryKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Discovery query key is required.", nameof(value));
        }

        Value = value.Trim();
    }

    public string Value { get; }

    public static DiscoveryQueryKey Search(string type, string normalizedQuery) =>
        new($"search:{Require(type, nameof(type))}:{Require(normalizedQuery, nameof(normalizedQuery))}");

    public static DiscoveryQueryKey Artist(ArtistId artistId) => new($"artist:{artistId.Value}");

    public static DiscoveryQueryKey Album(AlbumId albumId) => new($"album:{albumId.Value}");

    public static DiscoveryQueryKey Track(TrackId trackId) => new($"track:{trackId.Value}");

    public override string ToString() => Value;

    public static implicit operator string(DiscoveryQueryKey queryKey) => queryKey.Value;

    private static string Require(string value, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"{paramName} is required.", paramName);
        }

        return value.Trim();
    }
}
