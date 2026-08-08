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
    public void Given_Two_Different_Dispatch_Commands_For_The_Same_Track_When_Creating_Worker_Commands_Then_The_Command_Id_Preserves_The_Dispatch_Command_Id()
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

        firstCommand.Id.Value.Should().StartWith("cmd-streaming-a:");
        secondCommand.Id.Value.Should().StartWith("cmd-streaming-b:");
        firstCommand.Id.Should().NotBe(secondCommand.Id);
    }

    [Fact]
    public void Given_A_Long_Search_Lookup_When_Creating_A_Command_Then_The_Command_Id_Fits_Service_Bus_Limits()
    {
        var request = LookupWorkReadyHandlerUnitTestEnvironment.CreateSearchRequest(
            commandId: MessageId.Deterministic(
                "DispatchLookupWork",
                "search:Midnight Signals Aurora Lane",
                "2026-07-30T06:53:57.7546680+00:00").Value);

        var command = (LookupMusicbrainzSearchResultsMessage)WorkerCommandFactory.Create(
            request,
            new LookupAttempt.MusicbrainzSearchCatalogItems(
                new SearchCriteria("Midnight Signals Aurora Lane", SearchType.Track),
                LookupPriorityBand.High));

        command.Id.Value.Length.Should().BeLessThanOrEqualTo(128);
        command.Id.Value.Should().StartWith($"{request.Id.Value}:");
    }
}
