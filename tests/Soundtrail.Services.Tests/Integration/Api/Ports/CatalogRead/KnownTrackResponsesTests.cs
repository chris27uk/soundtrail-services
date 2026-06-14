using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;
using Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead.Support;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownTrackResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogReadPortContractModes.All), MemberType = typeof(CatalogReadPortContractModes))]
    public async Task Given_A_Known_Track_When_Getting_The_Track_Then_Track_Metadata_Is_Returned(CatalogReadPortMode mode)
    {
        using var env = CatalogReadTestEnvironment.Create(mode);
        env.Seed();

        var result = await env.Port.GetTrackAsync(
            ArtistId.From("artist_the_killers"),
            AlbumId.From("album_hot_fuss"),
            TrackId.From("track_mr_brightside"),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.AlbumName.Should().Be("Hot Fuss");
    }
}
