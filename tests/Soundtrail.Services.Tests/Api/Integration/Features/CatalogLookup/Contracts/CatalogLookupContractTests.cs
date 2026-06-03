using FluentAssertions;
using Soundtrail.Services.Features.CatalogLookup.Models;

namespace Soundtrail.Services.Tests.Api.Integration.Features.CatalogLookup.Contracts;

public sealed class CatalogLookupContractTests
{
    [Fact]
    public async Task Given_A_Known_Isrc_When_A_Track_Is_Looked_Up_Then_The_Matching_Track_Is_Returned()
    {
        using var env = CatalogLookupTestEnvironment.Create();
        var track = ContractKnownTracks.MrBrightsideTrack();
        env.Seed(track);

        var actual = await env.Lookup.LookupAsync(
            CatalogLookupRequest.ByIsrc(track.Isrc!.Value),
            CancellationToken.None);

        actual.Should().BeEquivalentTo(track);
    }

    [Fact]
    public async Task Given_A_Known_Isrc_And_Matching_Duration_When_A_Track_Is_Looked_Up_Then_The_Matching_Track_Is_Returned()
    {
        using var env = CatalogLookupTestEnvironment.Create();
        var track = ContractKnownTracks.MrBrightsideTrack();
        env.Seed(track);

        var actual = await env.Lookup.LookupAsync(
            CatalogLookupRequest.Create(track.Isrc!.Value, null, null, null, 222000),
            CancellationToken.None);

        actual.Should().BeEquivalentTo(track);
    }
}
