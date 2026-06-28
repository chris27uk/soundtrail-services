using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Discovery.Commands;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Tests.Unit.InternalProjector.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.InternalProjector.Features.OnDiscoveryRequested;

public sealed class OnDiscoveryRequestedHandlerTests
{
    [Fact]
    public async Task Given_A_Discovery_Request_With_Playback_When_Handled_Then_SearchCatalogRequested_Is_Sent()
    {
        var env = OnDiscoveryRequestedHandlerTestEnvironment.Create();

        await env.Handler.Handle(env.CommandWithPlayback(), CancellationToken.None);

        env.Bus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<SearchCatalogRequested>()
            .Which.Should().BeEquivalentTo(new
            {
                SearchCriteria = MusicSearchCriteria.ByQuery("rare unknown song", SearchTypesFilter.Tracks),
                Playback = PlaybackProviderFilter.Parse("spotify,appleMusic"),
                TrustLevel = 1,
                RiskScore = 10,
                CorrelationId = CorrelationId.From("corr-1"),
                CommandId = CommandId.For("SearchCatalogRequested:search:track:rare unknown song:1")
            });
    }

    [Fact]
    public async Task Given_A_Discovery_Request_Without_Playback_When_Handled_Then_No_Command_Is_Sent()
    {
        var env = OnDiscoveryRequestedHandlerTestEnvironment.Create();

        await env.Handler.Handle(env.CommandWithoutPlayback(), CancellationToken.None);

        env.Bus.SentCommands.Should().BeEmpty();
    }
}
