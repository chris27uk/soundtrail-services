using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog;

public readonly record struct CatalogAlbumId(ArtistId ArtistId, AlbumId AlbumId) : IValueType
{
    public string StableValue => $"{Uri.EscapeDataString(ArtistId.Value)}|{Uri.EscapeDataString(AlbumId.Value)}";

    public static CatalogAlbumId From(ArtistId artistId, AlbumId albumId) => new(artistId, albumId);

    public static CatalogAlbumId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Catalog album id is required.", nameof(value));
        }

        var separatorIndex = value.IndexOf('|');
        if (separatorIndex <= 0 || separatorIndex == value.Length - 1)
        {
            throw new ArgumentException($"Catalog album id '{value}' is invalid.", nameof(value));
        }

        var artistId = Uri.UnescapeDataString(value[..separatorIndex]);
        var albumId = Uri.UnescapeDataString(value[(separatorIndex + 1)..]);
        return new CatalogAlbumId(ArtistId.From(artistId), AlbumId.From(albumId));
    }

    public override string ToString() => StableValue;
}
