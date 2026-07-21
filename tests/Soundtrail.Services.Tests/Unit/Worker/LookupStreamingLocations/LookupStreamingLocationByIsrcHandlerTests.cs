using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Domain.Common;

namespace Soundtrail.Services.Tests.Unit.Worker.LookupStreamingLocations;

public sealed class LookupStreamingLocationByIsrcHandlerTests
{
    [Fact]
    public async Task Given_A_Track_With_An_Isrc_When_Handling_Then_A_Succeeded_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateIsrcRequest(trackId: global::Soundtrail.Services.Tests.TestTrackIds.Value("streaming-track-01"));
        environment.ReadTrackForLookupPort.Result = LookupStreamingLocationsUnitTestEnvironment.CreateTrack(seed: "streaming-track-01");
        var subject = environment.CreateIsrcBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        var completed = environment.CommandBus.Messages.Single().Should().BeOfType<CatalogLookupCompleted>().Subject;
        var result = completed.Result.Should().BeOfType<LookupResult.Succeeded>().Subject;
        result.Context.OriginalCommandId.Should().Be(request.Id);
        result.Value.Should().BeOfType<LookedUpData.TrackStreamingLink>();
        environment.ReadStreamingLocationByProviderPort.RequestedIsrc.Should().Be("GBAYE2409901");
        environment.ReadStreamingLocationByProviderPort.RequestedProvider.Should().Be(ProviderName.Spotify);
    }

    [Fact]
    public async Task Given_A_Missing_Track_When_Handling_Then_A_Failed_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateIsrcRequest();
        environment.ReadTrackForLookupPort.Result = null;
        var subject = environment.CreateIsrcBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.Failed>().Subject;
        result.Reason.Should().Be("Track was not found for streaming lookup.");
    }

    [Fact]
    public async Task Given_A_Track_Without_An_Isrc_When_Handling_Then_A_Not_Found_Result_Is_Published()
    {
        var environment = LookupStreamingLocationsUnitTestEnvironment.Create();
        var request = environment.CreateIsrcRequest(trackId: global::Soundtrail.Services.Tests.TestTrackIds.Value("streaming-track-no-isrc"));
        environment.ReadTrackForLookupPort.Result = LookupStreamingLocationsUnitTestEnvironment.CreateTrack(
            seed: "streaming-track-no-isrc",
            isrc: null);
        var subject = environment.CreateIsrcBusinessSubject();

        await subject.Handle(request, CancellationToken.None);

        var result = environment.CommandBus.Messages.Single()
            .Should().BeOfType<CatalogLookupCompleted>().Subject.Result
            .Should().BeOfType<LookupResult.NotFound>().Subject;
        result.Reason.Should().Be("Track does not have an ISRC.");
    }
}
