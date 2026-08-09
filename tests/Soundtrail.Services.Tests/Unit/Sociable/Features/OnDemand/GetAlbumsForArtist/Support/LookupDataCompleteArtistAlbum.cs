using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Catalog.Albums;
using Soundtrail.Domain.Catalog.Artists;
using Soundtrail.Domain.Discovery;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetAlbumsForArtist.Support;

internal sealed record LookupDataCompleteArtistAlbum(CatalogDiscoveryEntry CatalogEntry)
{
    public static LookupDataCompleteArtistAlbum Create(
        ArtistId artistId,
        string albumTitle,
        DateOnly? releaseDate,
        DateTimeOffset catalogUpdatedAt,
        string? artworkUrl = null,
        string? sourceAlbumId = null)
    {
        var albumId = AlbumId.From(artistId.Value, sourceAlbumId ?? albumTitle.ToLowerInvariant().Replace(' ', '-'));
        var album = new Album(
            albumId,
            albumTitle,
            SourceSystemIdSet.FromLegacyMusicBrainz(sourceAlbumId ?? albumId.ArtistAlbumId),
            releaseDate,
            artworkUrl,
            catalogUpdatedAt);

        return new LookupDataCompleteArtistAlbum(
            new CatalogDiscoveryEntry(artistId, new CatalogItem.MusicAlbum(album)));
    }
}

internal static class LookupDataCompleteArtistAlbumScenarios
{
    public static ArtistId DefaultArtistId { get; } = ArtistId.From("artist-aurora-lane");

    public static LookupDataCompleteArtistAlbum MidnightSignals(DateTimeOffset catalogUpdatedAt) =>
        LookupDataCompleteArtistAlbum.Create(
            DefaultArtistId,
            "Midnight Signals",
            new DateOnly(2023, 11, 10),
            catalogUpdatedAt,
            artworkUrl: "https://cdn.soundtrail.test/albums/midnight-signals.jpg",
            sourceAlbumId: "mb-release-midnight-signals");

    public static LookupDataCompleteArtistAlbum StaticHearts(DateTimeOffset catalogUpdatedAt) =>
        LookupDataCompleteArtistAlbum.Create(
            DefaultArtistId,
            "Static Hearts",
            new DateOnly(2022, 9, 16),
            catalogUpdatedAt,
            sourceAlbumId: "mb-release-static-hearts");
}
