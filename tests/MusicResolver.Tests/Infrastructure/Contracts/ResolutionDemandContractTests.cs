using FluentAssertions;
using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Infrastructure.Tests.Contracts;

public sealed class ResolutionDemandContractTests
{
    public static IEnumerable<object[]> Modes()
    {
        yield return new object[] { StorageMode.Fake };
        yield return new object[] { StorageMode.AzureTable };
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Recording_The_Same_Query_Twice_Is_Deduplicated(StorageMode mode)
    {
        var env = ResolutionDemandTestEnvironment.Create(mode);
        var query = NormalizedSearchQuery.From(SearchQuery.From("rare unknown song"));

        var first = await env.Store.RecordDemandAsync(query, CancellationToken.None);
        var second = await env.Store.RecordDemandAsync(query, CancellationToken.None);

        second.Should().Be(first);
    }
}
