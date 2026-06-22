using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead.Support;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class RavenAlbumTests
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

    [Theory]
    [MemberData(nameof(CatalogReadPortContractModes.All), MemberType = typeof(CatalogReadPortContractModes))]
    public async Task Given_An_Album_With_A_Track_From_A_Different_Artist_Sharing_The_Same_Album_Id_When_Getting_The_Album_Then_Only_The_Requested_Artists_Tracks_Are_Returned(CatalogReadPortMode mode)
    {
        using var env = CatalogReadTestEnvironment.Create(mode);
        env.SeedCrossArtistAlbumTrackScenario();

        var result = await env.Port.GetAlbumAsync(ArtistId.From("artist_the_killers"), AlbumId.From("album_hot_fuss"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Tracks.Select(track => track.TrackId.Value).Should().BeEquivalentTo(["track_mr_brightside"]);
    }
}
