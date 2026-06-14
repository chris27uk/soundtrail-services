using FluentAssertions;
using Soundtrail.Services.Api;
using System.Reflection;

namespace Soundtrail.Services.Tests.Unit.Api.Infrastructure.Raven;

public sealed class CatalogDocumentIdentityResponsesTests
{
    [Fact]
    public void Given_An_Artist_Id_When_Building_An_Artist_Document_Id_Then_It_Uses_The_Catalog_Prefix()
    {
        var id = InvokeDocumentIdBuilder(CatalogArtistDocumentType, "artist_123");

        id.Should().Be("catalog/artists/artist_123");
    }

    [Fact]
    public void Given_An_Album_Id_When_Building_An_Album_Document_Id_Then_It_Uses_The_Catalog_Prefix()
    {
        var id = InvokeDocumentIdBuilder(CatalogAlbumDocumentType, "album_456");

        id.Should().Be("catalog/albums/album_456");
    }

    [Fact]
    public void Given_A_Track_Id_When_Building_A_Track_Document_Id_Then_It_Uses_The_Catalog_Prefix()
    {
        var id = InvokeDocumentIdBuilder(CatalogTrackDocumentType, "track_789");

        id.Should().Be("catalog/tracks/track_789");
    }

    [Fact]
    public void Given_A_Query_Key_When_Building_A_Discovery_Status_Document_Id_Then_It_Uses_The_Catalog_Prefix()
    {
        var id = InvokeDocumentIdBuilder(DiscoveryStatusDocumentType, "search:track:karma police");

        id.Should().Be("catalog/discovery-status/search:track:karma police");
    }

    private static string InvokeDocumentIdBuilder(Type documentType, string value) =>
        (string)documentType
            .GetMethod("GetDocumentId", BindingFlags.Public | BindingFlags.Static)!
            .Invoke(null, [value])!;

    private static readonly Assembly ApiAssembly = typeof(ApiAssemblyMarker).Assembly;

    private static readonly Type CatalogArtistDocumentType = ApiAssembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogArtistDocument", throwOnError: true)!;

    private static readonly Type CatalogAlbumDocumentType = ApiAssembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogAlbumDocument", throwOnError: true)!;

    private static readonly Type CatalogTrackDocumentType = ApiAssembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.CatalogTrackDocument", throwOnError: true)!;

    private static readonly Type DiscoveryStatusDocumentType = ApiAssembly
        .GetType("Soundtrail.Services.Api.Infrastructure.Raven.Documents.DiscoveryStatusDocument", throwOnError: true)!;
}
