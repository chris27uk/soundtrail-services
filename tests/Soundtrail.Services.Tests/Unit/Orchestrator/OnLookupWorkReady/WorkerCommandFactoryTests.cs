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
            new LookupAttempt.MusicbrainzSearchCatalogItems(
                new Soundtrail.Domain.Search.SearchCriteria("u2", SearchType.Artist),
                LookupPriorityBand.High));

        command.Should().BeOfType<LookupMusicbrainzSearchResultsMessage>();
        ((LookupMusicbrainzSearchResultsMessage)command).Id.Value.Should().Be("lookup:musicbrainz-search:search:u2");
    }

    [Fact]
    public void Given_A_Streaming_Location_Isrc_Lookup_When_Creating_A_Command_Then_A_Narrow_Command_Is_Returned()
    {
        var request = LookupWorkReadyHandlerUnitTestEnvironment.CreateStreamingLocationRequest();

        var command = WorkerCommandFactory.Create(
            request,
            new LookupAttempt.StreamingLocationByIsrc(
                TestTrackIds.Create("track-2901"),
                ProviderName.Spotify,
                LookupPriorityBand.Low));

        command.Should().BeOfType<LookupStreamingLocationByIsrcMessage>();
        ((LookupStreamingLocationByIsrcMessage)command).Id.Value.Should().Be($"lookup:streaming-isrc:Spotify:{TestTrackIds.Create("track-2901").Value}");
    }

    [Fact]
    public void Given_Two_Different_Dispatch_Commands_For_The_Same_Track_When_Creating_Worker_Commands_Then_The_Command_Id_Is_Stable()
    {
        var first = LookupWorkReadyHandlerUnitTestEnvironment.CreateStreamingLocationRequest(commandId: "cmd-streaming-a");
        var second = LookupWorkReadyHandlerUnitTestEnvironment.CreateStreamingLocationRequest(commandId: "cmd-streaming-b");
        var trackId = TestTrackIds.Create("track-2901");

        var firstCommand = (LookupStreamingLocationByTrackMetadataMessage)WorkerCommandFactory.Create(
            first,
            new LookupAttempt.StreamingLocationByTrackMetadata(trackId, ProviderName.Spotify, LookupPriorityBand.Low));
        var secondCommand = (LookupStreamingLocationByTrackMetadataMessage)WorkerCommandFactory.Create(
            second,
            new LookupAttempt.StreamingLocationByTrackMetadata(trackId, ProviderName.Spotify, LookupPriorityBand.Low));

        firstCommand.Id.Should().Be(secondCommand.Id);
    }
}
