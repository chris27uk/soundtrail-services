using FluentAssertions;
using Soundtrail.Domain.Catalog;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Unit.Api.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Api.Features.Search.KnownLocalCatalog;

public sealed class ResponsesTests
{
    [Fact]
    public async Task Given_A_Known_Local_Result_When_Searching_Then_The_Result_Is_Returned()
    {
        var env = SearchCatalogHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside"));

        response.Results.Should().ContainSingle();
        response.Results[0].Type.Should().Be(SearchResultType.Track);
        response.Results[0].Name.Should().Be("Mr. Brightside");
        response.Results[0].PlayabilityStatus.Should().Be(PlayabilityStatus.Playable);
    }

    [Fact]
    public async Task Given_A_Known_Local_Result_When_Searching_Then_Discovery_Does_Not_Request_Work()
    {
        var env = SearchCatalogHandlerTestEnvironment.WithKnownTrack();

        var response = await env.Handler.Handle(env.Request("mr brightside"));

        response.Discovery.WillBeLookedUp.Should().BeFalse();
        env.EnqueueMusicRequests.Requests.Should().BeEmpty();
    }
}
