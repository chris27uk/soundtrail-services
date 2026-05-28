using FluentAssertions;
using MusicResolver.Application.Search;
using MusicResolver.Domain.ValueTypes;

namespace MusicResolver.Infrastructure.Tests.Contracts;

public sealed class QueryCacheContractTests
{
    public static IEnumerable<object[]> Modes()
    {
        yield return new object[] { StorageMode.Fake };
        yield return new object[] { StorageMode.AzureTable };
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Stored_Response_Can_Be_Read_Back(StorageMode mode)
    {
        var env = QueryCacheTestEnvironment.Create(mode);

        var query = SearchQuery.From("mr brightside");
        var normalizedQuery = NormalizedSearchQuery.From(query);
        var response = SearchMusicResponse.Resolved(query, new[] { ContractKnownTracks.MrBrightside() });

        await env.Cache.StoreAsync(
            normalizedQuery,
            response,
            TimeSpan.FromHours(1),
            CancellationToken.None);

        var actual = await env.Cache.GetAsync(normalizedQuery, CancellationToken.None);

        actual.Should().BeEquivalentTo(response);
    }
}
