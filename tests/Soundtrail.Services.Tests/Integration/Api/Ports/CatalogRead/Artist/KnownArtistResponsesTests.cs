using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Services.Tests.Integration.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Integration.Api.Ports.CatalogRead.Artist;

[Collection(RavenEmbeddedCollection.Name)]
public sealed class KnownArtistResponsesTests
{
    [Theory]
    [MemberData(nameof(CatalogReadPortContractModes.All), MemberType = typeof(CatalogReadPortContractModes))]
    public async Task Given_A_Known_Artist_When_Getting_The_Artist_Then_Albums_Are_Returned(CatalogReadPortMode mode)
    {
        using var env = CatalogReadTestEnvironment.Create(mode);
        env.Seed();

        var result = await env.Port.GetArtistAsync(ArtistId.From("artist_the_killers"), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Albums.Should().ContainSingle();
    }
}
