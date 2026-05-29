using FluentAssertions;
using Soundtrail.Services.Features.CatalogLookup.Models;
using Soundtrail.Services.Features.Tracks;
using Soundtrail.Services.Tests.Integration.Features.Search.Contracts;

namespace Soundtrail.Services.Tests.Integration.Features.CatalogLookup.Contracts;

public sealed class CatalogLookupContractTests
{
    public static IEnumerable<object[]> Modes()
    {
        yield return new object[] { StorageMode.Fake };
        yield return new object[] { StorageMode.AzureTable };
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Known_Isrc_When_A_Track_Is_Looked_Up_Then_The_Matching_Track_Is_Returned(StorageMode mode)
    {
        var env = CatalogLookupTestEnvironment.Create(mode);
        var track = ContractKnownTracks.MrBrightsideTrack();
        env.Seed(track);

        var actual = await env.Lookup.LookupAsync(
            CatalogLookupRequest.ByIsrc(track.Isrc!.Value),
            CancellationToken.None);

        actual.Should().BeEquivalentTo(track);
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Known_Isrc_And_Matching_Duration_When_A_Track_Is_Looked_Up_Then_The_Matching_Track_Is_Returned(StorageMode mode)
    {
        var env = CatalogLookupTestEnvironment.Create(mode);
        var track = ContractKnownTracks.MrBrightsideTrack();
        env.Seed(track);

        var actual = await env.Lookup.LookupAsync(
            CatalogLookupRequest.Create(track.Isrc!.Value, null, null, null, 222000),
            CancellationToken.None);

        actual.Should().BeEquivalentTo(track);
    }
}
