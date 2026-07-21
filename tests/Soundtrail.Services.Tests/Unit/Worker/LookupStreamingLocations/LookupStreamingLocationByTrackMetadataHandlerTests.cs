using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupStreamingLocations;

public sealed class LookupStreamingLocationByTrackMetadataHandlerTests
{
    [Fact]
    public async Task Given_A_Track_With_Metadata_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateMetadataRequest(trackId: global::Soundtrail.Services.Tests.TestTrackIds.Value("streaming-track-02"));
        environment.ReadTrackForLookupPort.Result = LookupStreamingLocationsUnitTestEnvironment.CreateTrack(
            seed: "streaming-track-02",
            title: "Summer Lights",
            artistName: "Northbound");
        var subject = environment.CreateMetadataBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        var completed = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        completed.Result.Should().BeOfType<LookupResult.Succeeded>();
        environment.ReadStreamingLocationByProviderPort.RequestedArtistName.Should().Be("Northbound");
        environment.ReadStreamingLocationByProviderPort.RequestedTrackTitle.Should().Be("Summer Lights");
        environment.ReadStreamingLocationByProviderPort.RequestedProvider.Should().Be(ProviderName.AppleMusic);
    }

    [Fact]
    public async Task Given_A_Track_With_Incomplete_Metadata_When_Handling_Then_A_Not_Found_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateMetadataRequest(trackId: global::Soundtrail.Services.Tests.TestTrackIds.Value("streaming-track-03"));
        environment.ReadTrackForLookupPort.Result = LookupStreamingLocationsUnitTestEnvironment.CreateTrack(
            seed: "streaming-track-03",
            title: string.Empty,
            artistName: "Northbound");
        var subject = environment.CreateMetadataBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.NotFound>().Subject;
        result.Reason.Should().Be("Track metadata is incomplete for provider lookup.");
    }

    [Fact]
    public async Task Given_No_Provider_Link_When_Handling_Then_A_Not_Found_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateMetadataRequest(trackId: global::Soundtrail.Services.Tests.TestTrackIds.Value("streaming-track-04"));
        environment.ReadTrackForLookupPort.Result = LookupStreamingLocationsUnitTestEnvironment.CreateTrack(seed: "streaming-track-04");
        environment.ReadStreamingLocationByProviderPort.MetadataResult = null;
        var subject = environment.CreateMetadataBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.NotFound>().Subject;
        result.Reason.Should().Be("Streaming location was not found for the requested provider.");
    }
}
