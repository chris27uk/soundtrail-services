using FluentAssertions;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Integration.Features.Search.Contracts;

public sealed class ResolutionDemandContractTests
{
    public static IEnumerable<object[]> Modes()
    {
        yield return new object[] { StorageMode.Fake };
        yield return new object[] { StorageMode.AzureTable };
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_The_Same_Query_Is_Recorded_Twice_When_Demand_Is_Stored_Then_It_Is_Deduplicated(StorageMode mode)
    {
        var env = ResolutionDemandTestEnvironment.Create(mode);
        var query = NormalizedSearchQuery.From(SearchQuery.From("rare unknown song"));

        var first = await env.Store.RecordDemandAsync(query, CancellationToken.None);
        var second = await env.Store.RecordDemandAsync(query, CancellationToken.None);

        second.Should().Be(first);
    }
}
