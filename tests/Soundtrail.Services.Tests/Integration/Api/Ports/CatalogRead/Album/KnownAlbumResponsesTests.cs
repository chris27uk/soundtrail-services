using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead.Album;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownAlbumResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogReadPortContractModes.All), MemberType = typeof(CatalogReadPortContractModes))]
    public async Task Given_A_Known_Album_When_Getting_The_Album_Then_Tracks_Are_Returned(CatalogReadPortMode mode)
    {
        using var env = CatalogReadTestEnvironment.Create(mode);
        env.Seed();

        var result = await env.Port.GetAlbumAsync(ArtistId.From("artist_the_killers"), AlbumId.From("album_hot_fuss"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Tracks.Should().ContainSingle();
    }
}
