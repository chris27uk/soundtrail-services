using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Messages;

namespace Soundtrail.Services.Tests.Unit.Sociable.Features.GetTracksForArtist.Scenarios.StreamingLookupMissingTrack.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Isrc_Lookup_Result_Is_Failed()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForStreamingLookupMissingTrack();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        IsrcResult(environment).Should().BeOfType<LookupResult.Failed>()
            .Which.Reason.Should().Be("Track was not found for streaming lookup.");
    }

    private static LookupResult IsrcResult(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message =>
                message.Result is LookupResult.Failed failed &&
                failed.Context.OriginalCommandId ==
                    environment.SentMessages<LookupStreamingLocationByIsrcMessage>().First().Id)
            .Result;
}
