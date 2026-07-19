using Soundtrail.Contracts.Common;
using Soundtrail.Domain.Common;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Discovery.Planning;
using Soundtrail.Domain.Search;
using Soundtrail.Services.Enrichment.Orchestrator.Features.Processing.OnLookupWorkReady.Collaborators;

namespace Soundtrail.Services.Tests.Unit.Orchestrator.OnLookupWorkReady;

public sealed class WorkerCommandFactoryTests
{
    [Fact]
    public void Given_A_Search_Lookup_When_Creating_A_Command_Then_A_Search_Command_Is_Returned()
    {
        var request = LookupWorkReadyHandlerUnitTestEnvironment.CreateSearchRequest();

        var command = WorkerCommandFactory.Create(
            request,
            new PlannedLookup.MusicbrainzSearch(
                new Soundtrail.Domain.Search.SearchCriteria("u2", SearchType.Artist),
                LookupPriorityBand.High));

        command.Should().BeOfType<LookupMusicbrainzSearchResultsCommand>();
        ((LookupMusicbrainzSearchResultsCommand)command).CommandId.Value.Should().Be("cmd-search:musicbrainz-search");
    }

    [Fact]
    public void Given_A_Streaming_Location_Isrc_Lookup_When_Creating_A_Command_Then_A_Narrow_Command_Is_Returned()
    {
        var request = LookupWorkReadyHandlerUnitTestEnvironment.CreateStreamingLocationRequest();

        var command = WorkerCommandFactory.Create(
            request,
            new PlannedLookup.StreamingLocationByIsrc(
                TestTrackIds.Create("track-2901"),
                ProviderName.Spotify,
                LookupPriorityBand.Low));

        command.Should().BeOfType<LookupStreamingLocationByIsrcCommand>();
        ((LookupStreamingLocationByIsrcCommand)command).CommandId.Value.Should().Be("cmd-streaming:streaming-isrc:Spotify");
    }
}
