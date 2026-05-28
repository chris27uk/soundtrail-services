using FluentAssertions;
using Soundtrail.Services.Features.Search;
using Soundtrail.Services.Features.Search.Models;

namespace Soundtrail.Services.Tests.Integration.Features.Search.Contracts;

public sealed class QueryCacheContractTests
{
    public static IEnumerable<object[]> Modes()
    {
        yield return new object[] { StorageMode.Fake };
        yield return new object[] { StorageMode.AzureTable };
    }

    [Theory]
    [MemberData(nameof(Modes))]
    public async Task Given_A_Stored_Response_When_It_Is_Read_Back_Then_The_Same_Response_Is_Returned(StorageMode mode)
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
