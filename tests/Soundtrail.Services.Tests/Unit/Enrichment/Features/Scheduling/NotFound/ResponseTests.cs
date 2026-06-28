using FluentAssertions;
using Soundtrail.Contracts.Common;
using Soundtrail.Services.Enrichment.Orchestrator.Features.OnCatalogSearchRequested.Support;
using Soundtrail.Services.Enrichment.Orchestrator.Shared.Search;
using Soundtrail.Services.Tests.Unit.Enrichment.Infrastructure;

namespace Soundtrail.Services.Tests.Unit.Enrichment.Features.Scheduling.NotFound;

public sealed class ResponseTests
{
    [Fact]
    public async Task Given_A_Request_That_Cannot_Be_Resolved_When_Handled_Then_A_Metadata_Record_Command_Is_Sent()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.Fails();

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 100));

        env.CommandBus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<RecordTrackMetadataLookupRequestedCommand>();
    }

    [Fact]
    public async Task Given_A_Request_With_A_Weak_Top_Match_When_Handled_Then_A_Metadata_Record_Command_Is_Sent()
    {
        var env = CatalogSearchRequestedHandlerTestEnvironment.WithNoExistingCandidates();
        env.Search.ReturnMatches(new MusicCatalogMatch(MusicCatalogId.From("mc_track_1"), 0.79m));

        await env.Handler.Handle(env.Request("rare unknown song", trustLevel: 0, riskScore: 10));

        env.CommandBus.SentCommands.Should().ContainSingle()
            .Which.Should().BeOfType<RecordTrackMetadataLookupRequestedCommand>();
    }
}
