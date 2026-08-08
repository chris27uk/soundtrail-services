using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Search;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.Search.Support;

internal sealed record LookupDataCompleteSearchArtist(CatalogDiscoveryEntry CatalogEntry)
{
    public static LookupDataCompleteSearchArtist Create(
        ArtistId artistId,
        string artistName,
        string? artworkUrl = null) =>
        new(
            new CatalogDiscoveryEntry(
                artistId,
                new CatalogItem.MusicArtist(new Artist
                {
                    Id = artistId,
                    Name = ArtistName.From(artistName),
                    ImageUrl = artworkUrl
                })));
}

internal static class LookupDataCompleteSearchScenarios
{
    public static string DefaultQuery { get; } = "Aurora Lane";

    public static SearchType DefaultFilter { get; } = SearchType.Artist;

    public static SearchCriteria DefaultCriteria { get; } = new(DefaultQuery, DefaultFilter);

    public static ArtistId DefaultArtistId { get; } = ArtistId.From("artist-aurora-lane");

    public static LookupDataCompleteSearchArtist AuroraLane() =>
        LookupDataCompleteSearchArtist.Create(
            DefaultArtistId,
            DefaultQuery,
            artworkUrl: "https://cdn.soundtrail.test/artists/aurora-lane.jpg");
}
