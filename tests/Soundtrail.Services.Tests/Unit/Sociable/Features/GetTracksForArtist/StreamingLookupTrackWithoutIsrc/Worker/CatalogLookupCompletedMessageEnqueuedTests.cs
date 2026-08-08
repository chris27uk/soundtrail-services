using Soundtrail.Domain.Discovery;
using Soundtrail.Domain.Discovery.Aggregates;
using Soundtrail.Domain.Discovery.Messages;
using Soundtrail.Services.Api.Features.Catalog.GetTracksForArtist.Contract;

namespace Soundtrail.Services.Tests.Unit.Sociable.GetTracksForArtist.StreamingLookupTrackWithoutIsrc.Worker;

public sealed class CatalogLookupCompletedMessageEnqueuedTests
{
    [Fact]
    public async Task Then_The_Isrc_Lookup_Result_Is_Not_Found()
    {
        var environment = GetTracksForArtistSociableTestEnvironment.ForStreamingLookupTrackWithoutIsrc();

        await environment.ProjectOnChange(sut => sut.Handle(environment.CreateRequest()));

        IsrcResult(environment).Should().BeOfType<LookupResult.NotFound>()
            .Which.Reason.Should().Be("Track does not have an ISRC.");
    }

    private static LookupResult IsrcResult(GetTracksForArtistSociableTestEnvironment environment) =>
        environment.SentMessages<CatalogLookupCompleted>()
            .Single(message =>
                message.Result is LookupResult.NotFound notFound &&
                notFound.Reason == "Track does not have an ISRC." &&
                notFound.Context.OriginalCommandId ==
                    environment.SentMessages<LookupStreamingLocationByIsrcMessage>().First().Id)
            .Result;
}
