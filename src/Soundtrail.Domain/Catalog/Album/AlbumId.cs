using Soundtrail.Contracts.Common;

namespace Soundtrail.Domain.Catalog;

public readonly record struct AlbumId : IValueType
{
    private readonly string value;
    
    private AlbumId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Album id is required.", nameof(value));
        }
        
        var separatorIndex = value.IndexOf(':', StringComparison.Ordinal);
        this.ArtistId = value[..separatorIndex];
        this.ArtistAlbumId = value[(separatorIndex + 1)..];
        this.value = value;
    }

    public string ArtistId { get; }
    
    public string ArtistAlbumId { get; }

    public string StableValue => value;

    public static AlbumId From(string value) => new(value);

    public override string ToString() => value;

    public static implicit operator string(AlbumId albumId) => albumId.value;
}
